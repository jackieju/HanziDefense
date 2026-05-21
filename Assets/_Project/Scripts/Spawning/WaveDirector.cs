using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.Core;
using HanziZombieDefense.Difficulty;
using HanziZombieDefense.Hanzi.Data;

namespace HanziZombieDefense.Spawning
{
    /// <summary>
    /// Inline serializable wave used when no <c>WaveDefinition</c> ScriptableObject
    /// asset is wired up. Mirrors the SO shape but lives directly on the director.
    /// </summary>
    [System.Serializable]
    public sealed class WaveConfig
    {
        [SerializeField, Min(1)] private int zombieCount = 10;
        [SerializeField, Min(0.05f)] private float startInterval = 2.5f;
        [SerializeField, Min(0.05f)] private float endInterval = 1.0f;
        [SerializeField, Min(0f)] private float restTimeAfterWave = 5f;

        public int ZombieCount => zombieCount;
        public float StartInterval => startInterval;
        public float EndInterval => endInterval;
        public float SpawnInterval => startInterval;
        public float RestTimeAfterWave => restTimeAfterWave;
    }

    /// <summary>
    /// Sequences waves of zombies via a coroutine. Reads pacing/quotas from
    /// <see cref="DifficultyManager"/> and falls back to per-wave inline values
    /// when no manager is registered. Publishes
    /// <see cref="GameEvents.WaveStarted"/> / <see cref="GameEvents.WaveCompleted"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaveDirector : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private ZombieSpawner spawner;
        [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

        [Header("Waves")]
        [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();

        [SerializeField, Tooltip("If true, after the final wave the director loops back to the first wave with rising difficulty.")]
        private bool loopAfterFinalWave = true;

        [SerializeField, Tooltip("Auto-start the first wave on Start. Otherwise call StartWaves() externally.")]
        private bool autoStart = true;

        private DifficultyManager _difficulty;
        private Coroutine _routine;
        private int _currentWaveIndex;
        private bool _running;

        public int CurrentWaveNumber => _currentWaveIndex + 1;
        public bool IsRunning => _running;

        private void Start()
        {
            if (spawner == null) spawner = FindObjectOfType<ZombieSpawner>();

            if (spawnPoints.Count == 0)
            {
                spawnPoints.AddRange(FindObjectsOfType<SpawnPoint>());
            }

            ServiceLocator.TryGet(out _difficulty);

            if (autoStart) StartCoroutine(WaitForDatabaseThenStart());
        }

        private System.Collections.IEnumerator WaitForDatabaseThenStart()
        {
            var db = FindObjectOfType<HanziDatabase>();
            if (db != null)
            {
                yield return db.WaitUntilReady();
            }
            StartWaves();
        }

        private void OnDisable()
        {
            StopWaves();
        }

        /// <summary>Begin the wave sequence from the first wave.</summary>
        public void StartWaves()
        {
            if (_running) return;
            if (waves == null || waves.Count == 0)
            {
                Debug.LogWarning("[WaveDirector] No waves configured.");
                return;
            }
            if (spawner == null)
            {
                Debug.LogError("[WaveDirector] No ZombieSpawner assigned.");
                return;
            }
            if (spawnPoints.Count == 0)
            {
                Debug.LogError("[WaveDirector] No SpawnPoints registered.");
                return;
            }

            _currentWaveIndex = 0;
            _running = true;
            _routine = StartCoroutine(RunWaves());
        }

        /// <summary>Halt the active wave sequence. In-flight zombies remain alive.</summary>
        public void StopWaves()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            _running = false;
        }

        private IEnumerator RunWaves()
        {
            while (_running)
            {
                WaveConfig wave = waves[_currentWaveIndex];

                EventBus.Publish(new GameEvents.WaveStarted { waveNumber = CurrentWaveNumber });

                yield return SpawnWave(wave);

                EventBus.Publish(new GameEvents.WaveCompleted { waveNumber = CurrentWaveNumber });

                float rest = wave.RestTimeAfterWave;
                if (rest > 0f) yield return new WaitForSeconds(rest);

                _currentWaveIndex++;
                if (_currentWaveIndex >= waves.Count)
                {
                    if (!loopAfterFinalWave) break;
                    _currentWaveIndex = 0;
                }
            }

            _running = false;
            _routine = null;
        }

        private IEnumerator SpawnWave(WaveConfig wave)
        {
            int spawned = 0;
            while (spawned < wave.ZombieCount)
            {
                while (_difficulty != null && spawner.ActiveCount >= _difficulty.MaxConcurrentZombies)
                {
                    yield return null;
                }

                int hsk = _difficulty != null ? _difficulty.CurrentHSKLevel : 1;
                int maxStrokes = _difficulty != null ? _difficulty.MaxStrokeCount : 8;

                SpawnPoint point = spawnPoints[Random.Range(0, spawnPoints.Count)];
                spawner.SpawnZombie(point, hsk, maxStrokes);
                spawned++;

                float t = wave.ZombieCount > 1 ? (float)spawned / (wave.ZombieCount - 1) : 1f;
                float interval = Mathf.Lerp(wave.StartInterval, wave.EndInterval, t);

                if (_difficulty != null)
                    interval = Mathf.Min(interval, _difficulty.EffectiveSpawnInterval);

                yield return new WaitForSeconds(interval);
            }
        }
    }
}
