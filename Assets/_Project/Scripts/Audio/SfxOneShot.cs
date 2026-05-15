using System.Collections;
using UnityEngine;

namespace HanziZombieDefense.Audio
{
    /// <summary>
    /// Helper attached to each pooled SFX <see cref="AudioSource"/>. <see cref="Play"/>
    /// configures and starts playback, then a coroutine returns the source to the
    /// owning <see cref="AudioManager"/> after the clip's duration elapses.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxOneShot : MonoBehaviour
    {
        private AudioSource _source;
        private AudioManager _owner;
        private Coroutine _routine;

        public AudioSource Source => _source;
        public bool IsPlaying => _source != null && _source.isPlaying;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
        }

        /// <summary>Bind the manager that owns this one-shot's pool.</summary>
        public void Bind(AudioManager owner)
        {
            _owner = owner;
        }

        /// <summary>Configure and start playback. Auto-returns to the pool when finished.</summary>
        public void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null) return;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _source.clip = clip;
            _source.volume = Mathf.Clamp01(volume);
            _source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            _source.Play();

            float effectivePitch = Mathf.Max(0.1f, Mathf.Abs(_source.pitch));
            float duration = clip.length / effectivePitch;
            _routine = StartCoroutine(ReturnAfter(duration));
        }

        /// <summary>Stop immediately and return to the pool.</summary>
        public void StopAndReturn()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            _source.Stop();
            _source.clip = null;
            _owner?.ReturnOneShot(this);
        }

        private IEnumerator ReturnAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _routine = null;
            _source.Stop();
            _source.clip = null;
            _owner?.ReturnOneShot(this);
        }
    }
}
