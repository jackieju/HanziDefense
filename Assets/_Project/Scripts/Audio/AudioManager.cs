using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.ScriptableObjects;

namespace HanziZombieDefense.Audio
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) singleton driving all audio playback.
    /// Maintains a pool of <see cref="SfxOneShot"/> sources for fire-and-forget SFX
    /// and a dedicated <see cref="AudioSource"/> for music.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        public static AudioManager Instance => _instance;

        [Header("SFX Pool")]
        [SerializeField, Min(1), Tooltip("Number of pooled AudioSources used for one-shot SFX playback.")]
        private int sfxSourceCount = 10;

        [SerializeField, Range(0f, 1f)] private float sfxMasterVolume = 1f;

        [Header("Music")]
        [SerializeField, Tooltip("Dedicated AudioSource used for music. Created at runtime if null.")]
        private AudioSource musicSource;

        [SerializeField, Range(0f, 1f)] private float musicMasterVolume = 0.7f;

        [SerializeField, Min(0f), Tooltip("Crossfade duration when swapping music tracks (seconds).")]
        private float musicCrossfade = 0.5f;

        [Header("Optional Bank")]
        [SerializeField, Tooltip("Optional AudioBank used by string-id playback.")]
        private AudioBank bank;

        private readonly Stack<SfxOneShot> _idle = new Stack<SfxOneShot>();
        private readonly HashSet<SfxOneShot> _active = new HashSet<SfxOneShot>();
        private Transform _sfxRoot;
        private Coroutine _musicFadeRoutine;

        public float SfxMasterVolume
        {
            get => sfxMasterVolume;
            set => sfxMasterVolume = Mathf.Clamp01(value);
        }

        public float MusicMasterVolume
        {
            get => musicMasterVolume;
            set
            {
                musicMasterVolume = Mathf.Clamp01(value);
                if (musicSource != null) musicSource.volume = musicMasterVolume;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            BuildSfxPool();
            EnsureMusicSource();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void BuildSfxPool()
        {
            var rootGo = new GameObject("SFX_Pool");
            rootGo.transform.SetParent(transform, false);
            _sfxRoot = rootGo.transform;

            for (int i = 0; i < sfxSourceCount; i++)
            {
                var go = new GameObject($"SfxOneShot_{i}");
                go.transform.SetParent(_sfxRoot, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                var oneShot = go.AddComponent<SfxOneShot>();
                oneShot.Bind(this);
                go.SetActive(false);
                _idle.Push(oneShot);
            }
        }

        private void EnsureMusicSource()
        {
            if (musicSource != null) return;

            var go = new GameObject("Music_Source");
            go.transform.SetParent(transform, false);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicMasterVolume;
        }

        // ─────────────────────────── SFX ───────────────────────────

        /// <summary>Play <paramref name="clip"/> as a one-shot. No-op if the pool is exhausted.</summary>
        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            SfxOneShot shot = AcquireOneShot();
            if (shot == null)
            {
                Debug.LogWarning("[AudioManager] SFX pool exhausted; skipping clip.");
                return;
            }

            shot.gameObject.SetActive(true);
            shot.Play(clip, Mathf.Clamp01(volume) * sfxMasterVolume, pitch);
        }

        /// <summary>Play a clip looked up by id from the configured <see cref="AudioBank"/>.</summary>
        public void PlaySFX(string id)
        {
            if (bank == null)
            {
                Debug.LogWarning("[AudioManager] PlaySFX(string) called but no AudioBank is assigned.");
                return;
            }

            var entry = bank.GetEntry(id);
            if (entry == null || entry.Clip == null)
            {
                Debug.LogWarning($"[AudioManager] No audio entry '{id}' in bank.");
                return;
            }
            PlaySFX(entry.Clip, entry.Volume, entry.Pitch);
        }

        // Called by SfxOneShot when it finishes / is stopped.
        internal void ReturnOneShot(SfxOneShot shot)
        {
            if (shot == null) return;
            if (!_active.Remove(shot)) return;
            shot.gameObject.SetActive(false);
            _idle.Push(shot);
        }

        private SfxOneShot AcquireOneShot()
        {
            if (_idle.Count > 0)
            {
                var shot = _idle.Pop();
                _active.Add(shot);
                return shot;
            }
            return null;
        }

        // ─────────────────────────── Music ───────────────────────────

        /// <summary>Start (or crossfade to) <paramref name="clip"/> on the dedicated music source.</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;

            if (musicSource.clip == clip && musicSource.isPlaying) return;

            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);

            if (musicCrossfade <= 0f || !musicSource.isPlaying)
            {
                musicSource.clip = clip;
                musicSource.volume = musicMasterVolume;
                musicSource.Play();
            }
            else
            {
                _musicFadeRoutine = StartCoroutine(CrossfadeTo(clip));
            }
        }

        /// <summary>Stop music playback (with a fade if <see cref="musicCrossfade"/> > 0).</summary>
        public void StopMusic()
        {
            if (musicSource == null) return;
            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);

            if (musicCrossfade <= 0f)
            {
                musicSource.Stop();
                musicSource.clip = null;
                return;
            }

            _musicFadeRoutine = StartCoroutine(FadeOutAndStop());
        }

        private System.Collections.IEnumerator CrossfadeTo(AudioClip clip)
        {
            float startVol = musicSource.volume;
            float t = 0f;
            float half = musicCrossfade * 0.5f;

            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / half);
                yield return null;
            }

            musicSource.clip = clip;
            musicSource.Play();

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicMasterVolume, t / half);
                yield return null;
            }

            musicSource.volume = musicMasterVolume;
            _musicFadeRoutine = null;
        }

        private System.Collections.IEnumerator FadeOutAndStop()
        {
            float startVol = musicSource.volume;
            float t = 0f;
            while (t < musicCrossfade)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / musicCrossfade);
                yield return null;
            }
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = musicMasterVolume;
            _musicFadeRoutine = null;
        }
    }
}
