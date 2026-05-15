using UnityEngine;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.Zombies
{
    /// <summary>
    /// Hit-point container for a single zombie. Triggers state transition to
    /// <see cref="ZombieState.Dying"/> and publishes <see cref="GameEvents.ZombieKilled"/>
    /// when reduced to zero.
    /// </summary>
    public class ZombieHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 1f;
        [SerializeField] private Zombie zombie;

        private float _current;
        private bool _isDead;

        /// <summary>Current health, clamped to [0, MaxHealth].</summary>
        public float CurrentHealth => _current;

        /// <summary>Maximum health configured in the inspector.</summary>
        public float MaxHealth => maxHealth;

        /// <summary>True after <see cref="Die"/> has run.</summary>
        public bool IsDead => _isDead;

        private void Awake()
        {
            if (zombie == null) zombie = GetComponent<Zombie>();
        }

        /// <summary>Reset to full health. Called by <see cref="Zombie.OnSpawn"/>.</summary>
        public void ResetHealth()
        {
            _current = maxHealth;
            _isDead = false;
        }

        /// <summary>
        /// Apply <paramref name="amount"/> damage. If health reaches zero, calls <see cref="Die"/>.
        /// No-op if already dead or amount is non-positive.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead || amount <= 0f) return;
            _current = Mathf.Max(0f, _current - amount);
            if (_current <= 0f) Die();
        }

        /// <summary>
        /// Mark this zombie as dead, transition the state machine to Dying, and
        /// publish <see cref="GameEvents.ZombieKilled"/> so scoring and FX can react.
        /// </summary>
        public void Die()
        {
            if (_isDead) return;
            _isDead = true;
            _current = 0f;

            if (zombie != null)
            {
                zombie.TransitionTo(ZombieState.Dying);
            }

            EventBus.Publish(new GameEvents.ZombieKilled
            {
                Zombie = zombie,
                Character = zombie != null ? zombie.GetAssignedCharacter() : null,
                Position = transform.position
            });
        }
    }
}
