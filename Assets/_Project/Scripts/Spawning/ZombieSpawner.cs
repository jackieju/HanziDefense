using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.Core;
using HanziZombieDefense.Hanzi.Data;
using HanziZombieDefense.Pooling;
using HanziZombieDefense.Zombies;

namespace HanziZombieDefense.Spawning
{
    /// <summary>
    /// Owns the <see cref="ObjectPool{Zombie}"/> for zombie instances and exposes
    /// spawn entry points used by <see cref="WaveDirector"/>. Zombies are arranged
    /// in lanes (Left / Center / Right) in front of the static camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZombieSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField, Tooltip("Prefab with Zombie component on its root.")]
        private Zombie zombiePrefab;

        [SerializeField, Tooltip("HanziDatabase used to assign characters. If null, found in scene at Awake.")]
        private HanziDatabase hanziDatabase;

        [SerializeField, Tooltip("Parent transform for pooled zombies. Defaults to this object.")]
        private Transform poolRoot;

        [Header("Pool")]
        [SerializeField, Min(1)] private int poolSize = 30;

        private ObjectPool<Zombie> _pool;
        private int _activeCount;
        private readonly List<SpawnPoint> _laneScratch = new List<SpawnPoint>(8);

        public int ActiveCount => _pool != null ? _pool.ActiveCount : _activeCount;
        public ObjectPool<Zombie> Pool => _pool;

        private void Awake()
        {
            if (zombiePrefab == null)
            {
                Debug.LogError("[ZombieSpawner] zombiePrefab is not assigned.");
                return;
            }

            if (poolRoot == null) poolRoot = transform;

            if (hanziDatabase == null)
            {
                hanziDatabase = FindObjectOfType<HanziDatabase>();
            }

            _pool = new ObjectPool<Zombie>(zombiePrefab, poolRoot, poolSize);

            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<ZombieSpawner>(out var registered) && registered == this)
            {
                ServiceLocator.Unregister<ZombieSpawner>();
            }
        }

        /// <summary>
        /// Spawn a zombie at <paramref name="point"/>, request a character from
        /// <see cref="HanziDatabase"/> matching the given filters, and publish
        /// <see cref="GameEvents.ZombieSpawned"/>. Returns the spawned zombie,
        /// or null if the pool/prefab is misconfigured.
        /// </summary>
        [Header("Debug")]
        [SerializeField, Tooltip("If set, all zombies spawn with this character instead of random. Clear for normal behavior.")]
        private string debugForceCharacter = "么";

        public Zombie SpawnZombie(SpawnPoint point, int hskLevel, int maxStrokes)
        {
            if (_pool == null)
            {
                Debug.LogError("[ZombieSpawner] Pool not initialized.");
                return null;
            }
            if (point == null)
            {
                Debug.LogError("[ZombieSpawner] SpawnZombie called with null SpawnPoint.");
                return null;
            }

            Zombie zombie = _pool.Get();
            zombie.transform.SetPositionAndRotation(point.GetSpawnPosition(), point.transform.rotation);
            zombie.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(debugForceCharacter) && hanziDatabase != null)
            {
                hanziDatabase.GetCharacterAsync(debugForceCharacter).ContinueWith(task =>
                {
                    if (task.Result != null && zombie != null)
                    {
                        zombie.AssignCharacter(task.Result);
                        zombie.gameObject.SetActive(true);
                        EventBus.Publish(new GameEvents.ZombieSpawned { zombie = zombie });
                    }
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            else if (hanziDatabase != null)
            {
                hanziDatabase.GetRandom(maxStrokes, hskLevel, character =>
                {
                    if (zombie != null && character != null)
                    {
                        zombie.AssignCharacter(character);
                        zombie.gameObject.SetActive(true);
                        EventBus.Publish(new GameEvents.ZombieSpawned { zombie = zombie });
                    }
                });
            }
            else
            {
                zombie.gameObject.SetActive(true);
                EventBus.Publish(new GameEvents.ZombieSpawned { zombie = zombie });
                Debug.LogWarning("[ZombieSpawner] No HanziDatabase available — zombie will spawn without a character.");
            }

            _activeCount++;
            return zombie;
        }

        /// <summary>
        /// Spawn a zombie in <paramref name="lane"/>, picking a random spawn point
        /// from <paramref name="candidates"/> that matches the requested lane.
        /// Falls back to any candidate when the lane has no matching point.
        /// </summary>
        public Zombie SpawnZombieInLane(SpawnLane lane, IList<SpawnPoint> candidates, int hskLevel, int maxStrokes)
        {
            if (candidates == null || candidates.Count == 0)
            {
                Debug.LogError("[ZombieSpawner] SpawnZombieInLane called with empty candidate list.");
                return null;
            }

            _laneScratch.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                SpawnPoint p = candidates[i];
                if (p != null && p.Lane == lane) _laneScratch.Add(p);
            }

            SpawnPoint chosen;
            if (_laneScratch.Count > 0)
            {
                chosen = _laneScratch[Random.Range(0, _laneScratch.Count)];
            }
            else
            {
                chosen = candidates[Random.Range(0, candidates.Count)];
            }

            return SpawnZombie(chosen, hskLevel, maxStrokes);
        }

        /// <summary>Return a zombie to the pool. Safe to call from anywhere.</summary>
        public void Despawn(Zombie zombie)
        {
            if (zombie == null || _pool == null) return;
            _pool.Return(zombie);
            _activeCount = Mathf.Max(0, _activeCount - 1);
        }

        /// <summary>Force-recall every active zombie. Useful between waves / on game over.</summary>
        public void DespawnAll()
        {
            _pool?.ReturnAll();
            _activeCount = 0;
        }
    }
}
