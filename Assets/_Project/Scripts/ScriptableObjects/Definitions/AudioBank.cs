using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.ScriptableObjects
{
    /// <summary>One named clip in an <see cref="AudioBank"/>.</summary>
    [Serializable]
    public sealed class AudioEntry
    {
        [SerializeField] private string id;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float pitch = 1f;

        public string Id => id;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float Pitch => pitch;
    }

    /// <summary>String-keyed registry of <see cref="AudioClip"/> assets used by AudioManager.</summary>
    [CreateAssetMenu(
        fileName = "AudioBank",
        menuName = "HanziZombieDefense/Audio Bank",
        order = 130)]
    public sealed class AudioBank : ScriptableObject
    {
        [SerializeField] private List<AudioEntry> entries = new List<AudioEntry>();

        private Dictionary<string, AudioEntry> _lookup;

        public IReadOnlyList<AudioEntry> Entries => entries;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, AudioEntry>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.Id)) continue;
                _lookup[e.Id] = e;
            }
        }

        /// <summary>Resolve the clip registered under <paramref name="id"/>, or null if missing.</summary>
        public AudioClip GetClip(string id)
        {
            var entry = GetEntry(id);
            return entry?.Clip;
        }

        /// <summary>Resolve the full entry (clip + volume + pitch) for <paramref name="id"/>.</summary>
        public AudioEntry GetEntry(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_lookup == null) BuildLookup();
            _lookup.TryGetValue(id, out var entry);
            return entry;
        }
    }
}
