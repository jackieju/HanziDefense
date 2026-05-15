using System;
using UnityEngine;
using HanziZombieDefense.Core;
using HanziZombieDefense.Hanzi.Data;
using HanziZombieDefense.Pooling;

namespace HanziZombieDefense.Zombies
{
    /// <summary>
    /// Discrete states a <see cref="Zombie"/> can be in.
    /// </summary>
    public enum ZombieState
    {
        Spawning,
        Approaching,
        Attacking,
        Dying,
        Dead
    }

    /// <summary>
    /// Root coordinator for a zombie instance. Owns the state machine, holds
    /// references to sibling components, and exposes pooling hooks.
    /// </summary>
    [DisallowMultipleComponent]
    public class Zombie : MonoBehaviour, IPoolable
    {
        /// <summary>
        /// Reference to the ZombieDefinition ScriptableObject (stats, prefab, speed, etc.).
        /// Typed as the base <see cref="ScriptableObject"/> until the concrete
        /// <c>ZombieDefinition</c> asset type is introduced.
        /// </summary>
        [SerializeField, Tooltip("ZombieDefinition asset (ScriptableObject) describing stats, prefab, etc.")]
        private ScriptableObject definition;

        [SerializeField] private ZombieHealth health;
        [SerializeField] private ZombieAI ai;
        [SerializeField] private ZombieAnimator zombieAnimator;
        [SerializeField] private ZombieCharacterLabel label;
        [SerializeField] private ZombieDeathFX deathFx;

        [SerializeField, Tooltip("Seconds spent in the Spawning state before transitioning to Approaching.")]
        private float spawnDuration = 0.75f;

        private ZombieState _state = ZombieState.Dead;
        private float _stateEnteredAt;
        private HanziCharacter _assignedCharacter;

        /// <summary>Fires every time <see cref="State"/> changes. Args: (previous, current).</summary>
        public event Action<ZombieState, ZombieState> StateChanged;

        /// <summary>Current state in the zombie lifecycle.</summary>
        public ZombieState State => _state;

        /// <summary>The ZombieDefinition ScriptableObject this instance was spawned from.</summary>
        public ScriptableObject Definition => definition;

        /// <summary>Convenience refs for siblings.</summary>
        public ZombieHealth Health => health;
        public ZombieAI AI => ai;
        public ZombieAnimator Animator => zombieAnimator;
        public ZombieCharacterLabel Label => label;
        public ZombieDeathFX DeathFx => deathFx;

        /// <summary>True while the zombie can be selected as a target by the player.</summary>
        public bool IsTargetable => _state == ZombieState.Approaching || _state == ZombieState.Attacking;

        private void Reset()
        {
            health = GetComponent<ZombieHealth>();
            ai = GetComponent<ZombieAI>();
            zombieAnimator = GetComponent<ZombieAnimator>();
            label = GetComponentInChildren<ZombieCharacterLabel>(true);
            deathFx = GetComponent<ZombieDeathFX>();
        }

        /// <summary>Pool callback: called when this zombie is taken from the pool.</summary>
        public void OnSpawn()
        {
            _assignedCharacter = null;
            if (label != null) label.SetCharacter(string.Empty);
            if (label != null) label.SetHighlighted(false);
            if (health != null) health.ResetHealth();
            TransitionTo(ZombieState.Spawning);
        }

        /// <summary>Pool callback: called right before this zombie returns to the pool.</summary>
        public void OnDespawn()
        {
            if (ai != null) ai.StopAI();
            _assignedCharacter = null;
            _state = ZombieState.Dead;
        }

        private void Update()
        {
            if (_state == ZombieState.Spawning && Time.time - _stateEnteredAt >= spawnDuration)
            {
                TransitionTo(ZombieState.Approaching);
            }
        }

        /// <summary>
        /// Move the state machine to <paramref name="next"/>. Idempotent: same-state
        /// transitions are ignored. Fires <see cref="StateChanged"/>.
        /// </summary>
        public void TransitionTo(ZombieState next)
        {
            if (_state == next) return;
            ZombieState previous = _state;
            _state = next;
            _stateEnteredAt = Time.time;
            StateChanged?.Invoke(previous, next);
        }

        /// <summary>
        /// Bind the hanzi character this zombie represents. Updates the world-space
        /// label and is required before the zombie becomes a valid target.
        /// </summary>
        public void AssignCharacter(HanziCharacter character)
        {
            _assignedCharacter = character;
            if (label != null && character != null)
            {
                label.SetCharacter(character.Character);
            }
        }

        /// <summary>The HanziCharacter currently bound, or null if none assigned.</summary>
        public HanziCharacter GetAssignedCharacter() => _assignedCharacter;

        /// <summary>
        /// Force-kill this zombie. Routes through <see cref="ZombieHealth.Die"/> so
        /// score events and death FX run uniformly.
        /// </summary>
        public void Kill()
        {
            if (_state == ZombieState.Dying || _state == ZombieState.Dead) return;
            if (health != null) health.Die();
            else TransitionTo(ZombieState.Dying);
        }
    }
}
