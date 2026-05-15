using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace HanziZombieDefense.Hanzi.Data
{
    /// <summary>
    /// Index entry produced by the build-time tooling that scans
    /// <c>StreamingAssets/Hanzi/graphics/</c> and emits <c>hanzi_index.json</c>.
    /// </summary>
    [Serializable]
    public class HanziIndexEntry
    {
        public string character;
        public int hsk;
        public int strokes;
    }

    /// <summary>JsonUtility wrapper for the index file.</summary>
    [Serializable]
    public class HanziIndexFile
    {
        public List<HanziIndexEntry> entries;
    }

    /// <summary>
    /// Runtime catalog of available characters. Owns:
    ///   1. an in-memory index (HSK level, stroke count) used to satisfy queries,
    ///   2. an LRU cache of fully-parsed <see cref="HanziCharacter"/> objects,
    ///   3. a small "warm" preload set that is guaranteed-resident to absorb
    ///      sub-second selection latency during a wave.
    /// Lazy-loads individual characters from <c>StreamingAssets/Hanzi/graphics/{char}.json</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HanziDatabase : MonoBehaviour
    {
        [Header("Paths (relative to StreamingAssets)")]
        [SerializeField] private string indexPath = "Hanzi/hanzi_index.json";
        [SerializeField] private string graphicsFolder = "Hanzi/graphics";

        [Header("Cache")]
        [Tooltip("Maximum number of fully-parsed characters held in the LRU cache.")]
        [SerializeField] private int cacheCapacity = 256;

        [Tooltip("Number of low-stroke-count characters to preload at init for instant access.")]
        [SerializeField] private int preloadCount = 24;

        [Tooltip("Cap stroke count for the preload pool — keep wave-1 zombies fast to spawn.")]
        [SerializeField] private int preloadMaxStrokes = 6;

        private readonly List<HanziIndexEntry> _index = new List<HanziIndexEntry>(2048);

        // LRU: LinkedList tracks recency; Dictionary maps key → node for O(1) lookup.
        private readonly Dictionary<string, LinkedListNode<CacheEntry>> _cacheLookup
            = new Dictionary<string, LinkedListNode<CacheEntry>>();
        private readonly LinkedList<CacheEntry> _cacheOrder = new LinkedList<CacheEntry>();

        // Pre-warmed set of characters that were eagerly loaded on init.
        private readonly Dictionary<string, HanziCharacter> _preloaded
            = new Dictionary<string, HanziCharacter>();

        private readonly System.Random _rng = new System.Random();

        /// <summary>True once the index file has been read and preloads have completed.</summary>
        public bool IsReady { get; private set; }

        /// <summary>Total number of characters known to this database.</summary>
        public int IndexCount => _index.Count;

        private struct CacheEntry
        {
            public string Key;
            public HanziCharacter Character;
        }

        // ─────────────────────────── Lifecycle ───────────────────────────

        private void Awake()
        {
            // Fire-and-forget: callers must wait on IsReady (or use IsReadyCoroutine).
            _ = InitializeAsync();
        }

        /// <summary>
        /// Load the index and preload a slice of low-stroke-count characters.
        /// Safe to await externally if a system needs to gate on readiness.
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadIndexAsync();
            await PreloadAsync();
            IsReady = true;
        }

        /// <summary>Coroutine convenience for non-async callers (UI bootstrap, etc.).</summary>
        public IEnumerator WaitUntilReady()
        {
            while (!IsReady) yield return null;
        }

        // ─────────────────────────── Public API ───────────────────────────

        /// <summary>
        /// Pick a random character matching the constraints and deliver it via
        /// <paramref name="onLoaded"/>. Uses the preload pool when possible to
        /// avoid I/O hitches during gameplay.
        /// </summary>
        /// <param name="maxStrokeCount">Inclusive upper bound on stroke count, or 0 to ignore.</param>
        /// <param name="hskLevel">Exact HSK level to filter by, or 0 to ignore.</param>
        /// <param name="onLoaded">Callback invoked on the main thread with the result (or null on failure).</param>
        public void GetRandom(int maxStrokeCount, int hskLevel, Action<HanziCharacter> onLoaded)
        {
            if (onLoaded == null) throw new ArgumentNullException(nameof(onLoaded));

            if (!IsReady)
            {
                Debug.LogWarning("[HanziDatabase] GetRandom called before init complete.");
                onLoaded(null);
                return;
            }

            // Fast path: try the preloaded pool first.
            var preloadHit = PickFromPreloaded(maxStrokeCount, hskLevel);
            if (preloadHit != null)
            {
                onLoaded(preloadHit);
                return;
            }

            var entry = PickIndexEntry(maxStrokeCount, hskLevel);
            if (entry == null)
            {
                Debug.LogWarning(
                    $"[HanziDatabase] No character matches strokes≤{maxStrokeCount} hsk={hskLevel}.");
                onLoaded(null);
                return;
            }

            _ = LoadAndDeliverAsync(entry.character, onLoaded);
        }

        /// <summary>
        /// Async variant of <see cref="GetRandom"/>. Returns null if no entry matches
        /// or the requested file fails to load.
        /// </summary>
        public async Task<HanziCharacter> GetRandomAsync(int maxStrokeCount, int hskLevel)
        {
            if (!IsReady) await WaitForReadyAsync();

            var preloadHit = PickFromPreloaded(maxStrokeCount, hskLevel);
            if (preloadHit != null) return preloadHit;

            var entry = PickIndexEntry(maxStrokeCount, hskLevel);
            if (entry == null) return null;

            return await GetCharacterAsync(entry.character);
        }

        /// <summary>Look up a specific character (cache → preload → file).</summary>
        public async Task<HanziCharacter> GetCharacterAsync(string character)
        {
            if (string.IsNullOrEmpty(character))
                throw new ArgumentException("Character is null or empty.", nameof(character));

            if (_preloaded.TryGetValue(character, out var preloaded))
                return preloaded;

            if (TryGetFromCache(character, out var cached))
                return cached;

            var loaded = await LoadCharacterFromStreamingAssetsAsync(character);
            if (loaded != null) AddToCache(character, loaded);
            return loaded;
        }

        // ─────────────────────────── Selection ───────────────────────────

        private HanziCharacter PickFromPreloaded(int maxStrokeCount, int hskLevel)
        {
            if (_preloaded.Count == 0) return null;

            var matches = new List<HanziCharacter>(_preloaded.Count);
            foreach (var kv in _preloaded)
            {
                var c = kv.Value;
                if (maxStrokeCount > 0 && c.StrokeCount > maxStrokeCount) continue;
                if (hskLevel > 0 && !MatchesHsk(c.Character, hskLevel)) continue;
                matches.Add(c);
            }

            if (matches.Count == 0) return null;
            return matches[_rng.Next(matches.Count)];
        }

        private bool MatchesHsk(string character, int hskLevel)
        {
            for (int i = 0; i < _index.Count; i++)
            {
                if (_index[i].character == character)
                    return _index[i].hsk == hskLevel;
            }
            return false;
        }

        private HanziIndexEntry PickIndexEntry(int maxStrokeCount, int hskLevel)
        {
            // Reservoir sampling avoids allocating a full filtered list for large indices.
            HanziIndexEntry chosen = null;
            int seen = 0;

            for (int i = 0; i < _index.Count; i++)
            {
                var e = _index[i];
                if (maxStrokeCount > 0 && e.strokes > maxStrokeCount) continue;
                if (hskLevel > 0 && e.hsk != hskLevel) continue;

                seen++;
                if (_rng.Next(seen) == 0) chosen = e;
            }

            return chosen;
        }

        // ─────────────────────────── Loading ───────────────────────────

        private async Task LoadIndexAsync()
        {
            var path = Path.Combine(Application.streamingAssetsPath, indexPath);
            string json = await ReadStreamingAssetTextAsync(path);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[HanziDatabase] Failed to read index at {path}.");
                return;
            }

            try
            {
                var file = JsonUtility.FromJson<HanziIndexFile>(json);
                if (file?.entries != null)
                {
                    _index.Clear();
                    _index.AddRange(file.entries);
                    Debug.Log($"[HanziDatabase] Loaded {_index.Count} entries from index.");
                }
                else
                {
                    Debug.LogError("[HanziDatabase] Index parsed but entries is null. Check JSON format.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HanziDatabase] Index parse failed: {ex.Message}");
            }
        }

        private async Task PreloadAsync()
        {
            if (preloadCount <= 0 || _index.Count == 0) return;

            var candidates = new List<HanziIndexEntry>(preloadCount * 2);
            for (int i = 0; i < _index.Count && candidates.Count < preloadCount; i++)
            {
                if (_index[i].strokes > 0 && _index[i].strokes <= preloadMaxStrokes)
                    candidates.Add(_index[i]);
            }

            foreach (var entry in candidates)
            {
                if (_preloaded.ContainsKey(entry.character)) continue;
                var ch = await LoadCharacterFromStreamingAssetsAsync(entry.character);
                if (ch != null) _preloaded[entry.character] = ch;
            }
        }

        private async Task LoadAndDeliverAsync(string character, Action<HanziCharacter> onLoaded)
        {
            var ch = await GetCharacterAsync(character);
            onLoaded(ch);
        }

        private async Task WaitForReadyAsync()
        {
            // Poll on a short delay rather than tying into the SynchronizationContext.
            while (!IsReady) await Task.Yield();
        }

        private async Task<HanziCharacter> LoadCharacterFromStreamingAssetsAsync(string character)
        {
            string codepoint = char.ConvertToUtf32(character, 0).ToString("x");
            var path = Path.Combine(
                Application.streamingAssetsPath,
                graphicsFolder,
                $"{codepoint}.json");

            string json = await ReadStreamingAssetTextAsync(path);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[HanziDatabase] Could not read character JSON at {path}.");
                return null;
            }

            try
            {
                return HanziDataLoader.LoadCharacterFromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HanziDatabase] Failed to parse '{character}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cross-platform StreamingAssets reader: WebGL / Android need
        /// UnityWebRequest, desktop platforms can use plain File I/O.
        /// </summary>
        private static async Task<string> ReadStreamingAssetTextAsync(string path)
        {
            // Android assets live inside the APK; only UnityWebRequest can resolve them.
            // The same path is fine on iOS/desktop, just slightly slower than File.ReadAllText.
            bool needsWebRequest =
                path.Contains("://") ||
                Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.WebGLPlayer;

            if (!needsWebRequest && File.Exists(path))
            {
                using var reader = new StreamReader(path);
                return await reader.ReadToEndAsync();
            }

            using var req = UnityWebRequest.Get(path);
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success) return null;
#else
            if (req.isNetworkError || req.isHttpError) return null;
#endif
            return req.downloadHandler.text;
        }

        // ─────────────────────────── LRU cache ───────────────────────────

        private bool TryGetFromCache(string key, out HanziCharacter character)
        {
            if (_cacheLookup.TryGetValue(key, out var node))
            {
                _cacheOrder.Remove(node);
                _cacheOrder.AddFirst(node);
                character = node.Value.Character;
                return true;
            }
            character = null;
            return false;
        }

        private void AddToCache(string key, HanziCharacter character)
        {
            if (_cacheLookup.ContainsKey(key)) return;

            var node = new LinkedListNode<CacheEntry>(new CacheEntry
            {
                Key = key,
                Character = character
            });
            _cacheOrder.AddFirst(node);
            _cacheLookup[key] = node;

            while (_cacheLookup.Count > cacheCapacity && _cacheOrder.Last != null)
            {
                var lru = _cacheOrder.Last;
                _cacheOrder.RemoveLast();
                _cacheLookup.Remove(lru.Value.Key);
            }
        }
    }
}
