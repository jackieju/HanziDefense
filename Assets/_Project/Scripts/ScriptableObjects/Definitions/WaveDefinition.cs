using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.ScriptableObjects
{
    /// <summary>One row of <see cref="WaveDefinition.Entries"/>: how many of which zombie at what cadence.</summary>
    [Serializable]
    public sealed class WaveEntry
    {
        [SerializeField] private ZombieDefinition definition;
        [SerializeField, Min(1)] private int count = 5;
        [SerializeField, Min(0.05f)] private float spawnInterval = 1.5f;

        public ZombieDefinition Definition => definition;
        public int Count => count;
        public float SpawnInterval => spawnInterval;
    }

    /// <summary>Authoring asset describing a single wave of zombies in a level.</summary>
    [CreateAssetMenu(
        fileName = "WaveDefinition",
        menuName = "HanziZombieDefense/Wave Definition",
        order = 110)]
    public sealed class WaveDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int waveNumber = 1;
        [SerializeField] private List<WaveEntry> entries = new List<WaveEntry>();
        [SerializeField, Min(0f), Tooltip("Seconds of calm before the next wave begins.")]
        private float restTimeAfterWave = 5f;

        public int WaveNumber => waveNumber;
        public IReadOnlyList<WaveEntry> Entries => entries;
        public float RestTimeAfterWave => restTimeAfterWave;

        /// <summary>Sum of <see cref="WaveEntry.Count"/> across all entries.</summary>
        public int TotalZombieCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null) total += entries[i].Count;
                }
                return total;
            }
        }
    }
}
