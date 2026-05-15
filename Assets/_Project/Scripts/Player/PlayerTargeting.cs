using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.Core;
using HanziZombieDefense.Zombies;

namespace HanziZombieDefense.Player
{
    /// <summary>
    /// Auto-targets the FRONTMOST (nearest) targetable zombie. The mobile build
    /// is sequential — only one zombie is "active" at a time. When the active
    /// target dies, despawns, or leaves the targetable states, the next nearest
    /// zombie is acquired automatically. Selection is purely distance-based —
    /// no raycasts, no cones, no manual input. Publishes
    /// <see cref="GameEvents.TargetChanged"/> on every switch.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerTargeting : MonoBehaviour
    {
        [SerializeField, Tooltip("Origin used to measure distance to zombies. Defaults to this transform.")]
        private Transform aimSource;

        [SerializeField, Min(0f), Tooltip("Seconds between target re-scans. 0 = every frame.")]
        private float scanInterval = 0f;

        private readonly List<Zombie> _liveZombies = new List<Zombie>(64);

        private Zombie _activeTarget;
        private float _nextScanTime;

        /// <summary>The zombie currently being written against, or null if none.</summary>
        public Zombie ActiveTarget => _activeTarget;

        /// <summary>Backward-compatible alias for <see cref="ActiveTarget"/>.</summary>
        public Zombie CurrentTarget => _activeTarget;

        private void Awake()
        {
            if (aimSource == null) aimSource = transform;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.ZombieSpawned>(OnZombieSpawned);
            EventBus.Subscribe<GameEvents.ZombieKilled>(OnZombieKilled);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.ZombieSpawned>(OnZombieSpawned);
            EventBus.Unsubscribe<GameEvents.ZombieKilled>(OnZombieKilled);
            _liveZombies.Clear();
            _activeTarget = null;
        }

        private void Update()
        {
            if (scanInterval > 0f)
            {
                if (Time.time < _nextScanTime) return;
                _nextScanTime = Time.time + scanInterval;
            }

            Zombie nearest = FindNearestTargetable();
            if (!ReferenceEquals(nearest, _activeTarget))
            {
                SetTarget(nearest);
            }
        }

        private Zombie FindNearestTargetable()
        {
            Vector3 origin = aimSource.position;
            Zombie best = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = _liveZombies.Count - 1; i >= 0; i--)
            {
                Zombie z = _liveZombies[i];
                if (z == null || !z.isActiveAndEnabled)
                {
                    _liveZombies.RemoveAt(i);
                    continue;
                }
                if (!z.IsTargetable) continue;

                float sqr = (z.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = z;
                }
            }

            return best;
        }

        private void SetTarget(Zombie zombie)
        {
            if (ReferenceEquals(_activeTarget, zombie)) return;

            Zombie previous = _activeTarget;
            _activeTarget = zombie;

            EventBus.Publish(new GameEvents.TargetChanged
            {
                Previous = previous,
                Current = zombie,
                newTarget = zombie
            });
        }

        private void OnZombieSpawned(GameEvents.ZombieSpawned evt)
        {
            if (evt.zombie == null) return;
            if (!_liveZombies.Contains(evt.zombie))
                _liveZombies.Add(evt.zombie);
        }

        private void OnZombieKilled(GameEvents.ZombieKilled evt)
        {
            if (evt.Zombie == null) return;
            _liveZombies.Remove(evt.Zombie);
        }
    }
}
