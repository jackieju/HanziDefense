using System;
using System.Collections;
using UnityEngine;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.Player
{
    /// <summary>
    /// Tracks the player's hit points, applies invincibility frames after a hit,
    /// and broadcasts <see cref="GameEvents.PlayerDamaged"/> / <see cref="GameEvents.PlayerDied"/>
    /// through the global <see cref="EventBus"/>.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        [SerializeField, Tooltip("Seconds of invincibility after taking a hit. Prevents instant-death from a swarm of zombies all hitting in the same frame.")]
        private float iFrameDuration = 0.5f;

        private float _currentHealth;
        private float _iFrameEndsAt;
        private bool _isDead;

        /// <summary>Current hit points. Clamped to [0, MaxHealth].</summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>Maximum hit points configured in the inspector.</summary>
        public float MaxHealth => maxHealth;

        /// <summary>Health as a 0..1 fraction. Convenient for UI bars.</summary>
        public float Normalized => maxHealth > 0f ? _currentHealth / maxHealth : 0f;

        /// <summary>True after <see cref="TakeDamage"/> has driven health to zero.</summary>
        public bool IsDead => _isDead;

        /// <summary>True while invincibility frames are active.</summary>
        public bool IsInvincible => Time.time < _iFrameEndsAt;

        private void OnEnable()
        {
            _currentHealth = maxHealth;
            _isDead = false;
            _iFrameEndsAt = 0f;
        }

        /// <summary>
        /// Apply <paramref name="amount"/> damage. Ignored while dead or during i-frames.
        /// Publishes <see cref="GameEvents.PlayerDamaged"/>; if health hits zero,
        /// also publishes <see cref="GameEvents.PlayerDied"/>.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead || amount <= 0f) return;
            if (IsInvincible) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            _iFrameEndsAt = Time.time + iFrameDuration;

            EventBus.Publish(new GameEvents.PlayerDamaged
            {
                Amount = amount,
                CurrentHealth = _currentHealth,
                MaxHealth = maxHealth,
                remainingHP = _currentHealth
            });

            if (_currentHealth <= 0f)
            {
                _isDead = true;
                EventBus.Publish(new GameEvents.PlayerDied());
            }
        }

        /// <summary>
        /// Restore <paramref name="amount"/> hit points (clamped at MaxHealth).
        /// No-op if dead or amount &lt;= 0.
        /// </summary>
        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f) return;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
        }
    }
}
