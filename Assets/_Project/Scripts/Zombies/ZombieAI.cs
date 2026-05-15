using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using HanziZombieDefense.Core;
using HanziZombieDefense.Difficulty;

namespace HanziZombieDefense.Zombies
{
    /// <summary>
    /// Drives a <see cref="NavMeshAgent"/> toward the player. Repath cadence is
    /// throttled to <see cref="updateInterval"/> seconds to keep many zombies cheap.
    /// Speed is sourced from the zombie's definition (via reflection on a
    /// <c>BaseSpeed</c> field if present) multiplied by the active
    /// <see cref="DifficultyManager"/> speed multiplier.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : MonoBehaviour
    {
        [SerializeField, Tooltip("Seconds between SetDestination() calls. Higher = cheaper, less responsive.")]
        private float updateInterval = 0.25f;

        [SerializeField, Tooltip("Distance (m) at which the zombie stops moving and enters Attacking.")]
        private float attackRange = 1.5f;

        [SerializeField, Tooltip("Fallback walk speed if the definition does not expose one.")]
        private float fallbackSpeed = 2.5f;

        [SerializeField] private Zombie zombie;

        private NavMeshAgent _agent;
        private Transform _player;
        private Coroutine _tickRoutine;
        private float _baseSpeed;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (zombie == null) zombie = GetComponent<Zombie>();
            _agent.stoppingDistance = attackRange * 0.9f;
        }

        private void OnEnable()
        {
            _baseSpeed = ResolveBaseSpeed();
            ApplySpeed();
            _tickRoutine = StartCoroutine(Tick());
        }

        private void OnDisable()
        {
            StopAI();
        }

        /// <summary>Halts navigation and the repath coroutine. Called on despawn.</summary>
        public void StopAI()
        {
            if (_tickRoutine != null)
            {
                StopCoroutine(_tickRoutine);
                _tickRoutine = null;
            }
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.isStopped = true;
            }
        }

        private IEnumerator Tick()
        {
            var wait = new WaitForSeconds(updateInterval);
            while (true)
            {
                ApplySpeed();
                UpdateDestination();
                yield return wait;
            }
        }

        private void UpdateDestination()
        {
            if (zombie == null) return;
            if (zombie.State == ZombieState.Dying || zombie.State == ZombieState.Dead || zombie.State == ZombieState.Spawning)
            {
                if (_agent.isOnNavMesh) _agent.isStopped = true;
                return;
            }

            Transform player = ResolvePlayer();
            if (player == null) return;
            if (!_agent.isOnNavMesh) return;

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= attackRange)
            {
                _agent.isStopped = true;
                if (zombie.State != ZombieState.Attacking)
                {
                    zombie.TransitionTo(ZombieState.Attacking);
                }
            }
            else
            {
                _agent.isStopped = false;
                _agent.SetDestination(player.position);
                if (zombie.State == ZombieState.Attacking)
                {
                    zombie.TransitionTo(ZombieState.Approaching);
                }
            }
        }

        private void ApplySpeed()
        {
            float multiplier = 1f;
            if (ServiceLocator.TryGet<DifficultyManager>(out var difficulty))
            {
                multiplier = difficulty.SpeedMultiplier;
            }
            _agent.speed = Mathf.Max(0.1f, _baseSpeed * multiplier);
        }

        private float ResolveBaseSpeed()
        {
            ScriptableObject def = zombie != null ? zombie.Definition : null;
            if (def == null) return fallbackSpeed;

            // Reflectively read a public/private "BaseSpeed" or "baseSpeed" field/property
            // so we don't depend on a concrete ZombieDefinition type yet.
            var t = def.GetType();
            var prop = t.GetProperty("BaseSpeed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop != null && (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double)))
            {
                return System.Convert.ToSingle(prop.GetValue(def));
            }
            var field = t.GetField("baseSpeed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null && (field.FieldType == typeof(float) || field.FieldType == typeof(double)))
            {
                return System.Convert.ToSingle(field.GetValue(def));
            }
            return fallbackSpeed;
        }

        private Transform ResolvePlayer()
        {
            if (_player != null) return _player;
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
            return _player;
        }
    }
}
