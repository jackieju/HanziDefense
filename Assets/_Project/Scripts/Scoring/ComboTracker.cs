using System;
using UnityEngine;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.Scoring
{
    /// <summary>
    /// Tracks consecutive zombie kills without taking damage. Drives the score multiplier.
    /// Listens to <see cref="GameEvents.ZombieKilled"/> (increment) and
    /// <see cref="GameEvents.PlayerDamaged"/> (reset).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ComboTracker : MonoBehaviour
    {
        [Header("Multiplier")]
        [SerializeField, Min(0f), Tooltip("Combo multiplier = 1 + combo * comboMultiplierStep, capped at maxMultiplier.")]
        private float comboMultiplierStep = 0.1f;

        [SerializeField, Min(1f), Tooltip("Hard ceiling on the multiplier (e.g. 3x).")]
        private float maxMultiplier = 3f;

        public int CurrentCombo { get; private set; }
        public int HighestCombo { get; private set; }

        public float ComboMultiplier
        {
            get
            {
                float raw = 1f + CurrentCombo * comboMultiplierStep;
                return Mathf.Min(raw, maxMultiplier);
            }
        }

        /// <summary>Fires whenever the combo value changes (increment or reset).</summary>
        public event Action<int> ComboChanged;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.ZombieKilled>(OnZombieKilled);
            EventBus.Subscribe<GameEvents.PlayerDamaged>(OnPlayerDamaged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.ZombieKilled>(OnZombieKilled);
            EventBus.Unsubscribe<GameEvents.PlayerDamaged>(OnPlayerDamaged);
        }

        /// <summary>Add one to the current combo and update the high-water mark.</summary>
        public void IncrementCombo()
        {
            CurrentCombo++;
            if (CurrentCombo > HighestCombo) HighestCombo = CurrentCombo;
            ComboChanged?.Invoke(CurrentCombo);
        }

        /// <summary>Force the combo back to zero.</summary>
        public void ResetCombo()
        {
            if (CurrentCombo == 0) return;
            CurrentCombo = 0;
            ComboChanged?.Invoke(0);
        }

        private void OnZombieKilled(GameEvents.ZombieKilled _) => IncrementCombo();
        private void OnPlayerDamaged(GameEvents.PlayerDamaged _) => ResetCombo();
    }
}
