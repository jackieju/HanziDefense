using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using HanziZombieDefense.Audio;
using HanziZombieDefense.Hanzi.Data;

namespace HanziZombieDefense.Core
{
    /// <summary>
    /// First component executed by the Boot scene. Responsibilities:
    ///   1. Build the persistent root (DontDestroyOnLoad) and host long-lived services.
    ///   2. Instantiate <see cref="GameManager"/> and <see cref="AudioManager"/> if missing.
    ///   3. Spin up <see cref="HanziDatabase"/> and await its index load.
    ///   4. Register everything with <see cref="ServiceLocator"/>.
    ///   5. Load the MainMenu scene additively, then unload Boot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BootLoader : MonoBehaviour
    {
        private const string PersistentRootName = "--- Persistent ---";

        [Header("Scenes")]
        [Tooltip("Name of the scene to additively load once boot completes.")]
        [SerializeField] private string mainMenuSceneName = GameManager.MainMenuScene;

        [Tooltip("Name of this Boot scene. Unloaded after MainMenu finishes loading.")]
        [SerializeField] private string bootSceneName = GameManager.BootScene;

        [Header("Hanzi Database")]
        [Tooltip("StreamingAssets-relative path to the hanzi index file.")]
        [SerializeField] private string hanziIndexPath = "Hanzi/hanzi_index.json";

        [Tooltip("Abort boot if the hanzi index cannot be read. Disable to allow menu-only mode.")]
        [SerializeField] private bool requireHanziIndex = false;

        [Header("Optional Prefabs")]
        [Tooltip("Optional GameManager prefab. If null, a fresh GameObject is created.")]
        [SerializeField] private GameManager gameManagerPrefab;

        [Tooltip("Optional AudioManager prefab. If null, a fresh GameObject is created.")]
        [SerializeField] private AudioManager audioManagerPrefab;

        private Transform _persistentRoot;

        private void Awake()
        {
            // Avoid re-entry if Boot somehow gets reloaded.
            if (GameObject.Find(PersistentRootName) != null)
            {
                Debug.LogWarning("[BootLoader] Persistent root already exists; skipping boot.");
                return;
            }

            _ = BootAsync();
        }

        private async Task BootAsync()
        {
            try
            {
                _persistentRoot = CreatePersistentRoot();

                var gameManager = EnsureGameManager(_persistentRoot);
                var audioManager = EnsureAudioManager(_persistentRoot);
                var hanziDatabase = EnsureHanziDatabase(_persistentRoot);

                ServiceLocator.Register(gameManager);
                ServiceLocator.Register(audioManager);
                ServiceLocator.Register(hanziDatabase);

                bool indexOk = await PreloadHanziIndexAsync();
                if (!indexOk && requireHanziIndex)
                {
                    Debug.LogError("[BootLoader] Hanzi index missing; aborting boot.");
                    return;
                }

                // Wait for HanziDatabase to finish its own init (preload pool, etc.).
                while (!hanziDatabase.IsReady) await Task.Yield();

                await LoadMainMenuAndUnloadBootAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BootLoader] Boot failed: {ex}");
            }
        }

        // ─────────────────────────── Persistent root ───────────────────────────

        private static Transform CreatePersistentRoot()
        {
            var go = new GameObject(PersistentRootName);
            DontDestroyOnLoad(go);
            return go.transform;
        }

        // ─────────────────────────── Service bring-up ───────────────────────────

        private GameManager EnsureGameManager(Transform parent)
        {
            var existing = GameManager.Instance;
            if (existing != null) return existing;

            GameManager instance;
            if (gameManagerPrefab != null)
            {
                instance = Instantiate(gameManagerPrefab, parent);
                instance.name = nameof(GameManager);
            }
            else
            {
                var go = new GameObject(nameof(GameManager));
                go.transform.SetParent(parent, false);
                instance = go.AddComponent<GameManager>();
            }
            return instance;
        }

        private AudioManager EnsureAudioManager(Transform parent)
        {
            var existing = AudioManager.Instance;
            if (existing != null) return existing;

            AudioManager instance;
            if (audioManagerPrefab != null)
            {
                instance = Instantiate(audioManagerPrefab, parent);
                instance.name = nameof(AudioManager);
            }
            else
            {
                var go = new GameObject(nameof(AudioManager));
                go.transform.SetParent(parent, false);
                instance = go.AddComponent<AudioManager>();
            }
            return instance;
        }

        private static HanziDatabase EnsureHanziDatabase(Transform parent)
        {
            var existing = FindObjectOfType<HanziDatabase>();
            if (existing != null) return existing;

            var go = new GameObject(nameof(HanziDatabase));
            go.transform.SetParent(parent, false);
            return go.AddComponent<HanziDatabase>();
        }

        // ─────────────────────────── Hanzi index probe ───────────────────────────

        /// <summary>
        /// Sanity-check that the index file is readable. Actual parsing happens
        /// inside <see cref="HanziDatabase.InitializeAsync"/>; this is just a
        /// fast existence probe so we can fail fast if data wasn't shipped.
        /// </summary>
        private async Task<bool> PreloadHanziIndexAsync()
        {
            string path = Path.Combine(Application.streamingAssetsPath, hanziIndexPath);
            string json = await ReadStreamingAssetTextAsync(path);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[BootLoader] Hanzi index empty or missing at {path}.");
                return false;
            }
            return true;
        }

        private static async Task<string> ReadStreamingAssetTextAsync(string path)
        {
            // Android packs StreamingAssets inside the APK — must use UnityWebRequest.
            // WebGL paths are URLs; same story.
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

        // ─────────────────────────── Scene swap ───────────────────────────

        private async Task LoadMainMenuAndUnloadBootAsync()
        {
            if (string.IsNullOrEmpty(mainMenuSceneName))
            {
                Debug.LogError("[BootLoader] Main menu scene name is empty.");
                return;
            }

            var loadOp = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                Debug.LogError($"[BootLoader] Failed to start load of '{mainMenuSceneName}'. Is it in Build Settings?");
                return;
            }

            while (!loadOp.isDone) await Task.Yield();

            var mainMenuScene = SceneManager.GetSceneByName(mainMenuSceneName);
            if (mainMenuScene.IsValid())
            {
                SceneManager.SetActiveScene(mainMenuScene);
            }

            var bootScene = SceneManager.GetSceneByName(bootSceneName);
            if (bootScene.IsValid() && bootScene.isLoaded)
            {
                var unloadOp = SceneManager.UnloadSceneAsync(bootScene);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone) await Task.Yield();
                }
            }
        }

        /// <summary>Coroutine convenience for callers that prefer non-async wait.</summary>
        public IEnumerator WaitForServices()
        {
            while (!ServiceLocator.Has<HanziDatabase>()) yield return null;
            var db = ServiceLocator.Get<HanziDatabase>();
            while (!db.IsReady) yield return null;
        }
    }
}
