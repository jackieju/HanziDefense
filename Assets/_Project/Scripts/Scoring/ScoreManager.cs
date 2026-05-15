using System;
using UnityEngine;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.Scoring
{
    /// <summary>
    /// Authoritative score store. Subscribes to <see cref="GameEvents.ZombieKilled"/>
    /// and applies an accuracy bonus + combo multiplier from <see cref="ComboTracker"/>.
    /// Persists the high score in <see cref="PlayerPrefs"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScoreManager : MonoBehaviour
    {
        public const string HighScorePrefKey = "HanziZombieDefense.HighScore";

        [SerializeField] private ComboTracker comboTracker;

        [Header("Counters")]
        [SerializeField, Tooltip("Number of zombies killed in the current run.")]
        private int killsThisRun;

        private int _score;
        private int _highScore;

        public int Score => _score;
        public int HighScore => _highScore;
        public int KillsThisRun => killsThisRun;

        /// <summary>Fired whenever <see cref="Score"/> changes; payload is the new score.</summary>
        public event Action<int> ScoreChanged;

        private void Awake()
        {
            if (comboTracker == null) comboTracker = GetComponent<ComboTracker>();
            if (comboTracker == null) comboTracker = FindObjectOfType<ComboTracker>();

            _highScore = PlayerPrefs.GetInt(HighScorePrefKey, 0);

            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.ZombieKilled>(OnZombieKilled);
            EventBus.Subscribe<GameEvents.PlayerDied>(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.ZombieKilled>(OnZombieKilled);
            EventBus.Unsubscribe<GameEvents.PlayerDied>(OnPlayerDied);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<ScoreManager>(out var registered) && registered == this)
            {
                ServiceLocator.Unregister<ScoreManager>();
            }
        }

        public int GetScore() => _score;
        public int GetHighScore() => _highScore;

        /// <summary>
        /// Award score for a kill. Final delta = round(baseScore * (1 + accuracyBonus) * comboMultiplier).
        /// </summary>
        /// <param name="baseScore">Base value (typically <c>ZombieDefinition.ScoreValue</c>).</param>
        /// <param name="accuracyBonus">Bonus in [0, ∞). 0 = no bonus, 0.5 = +50%.</param>
        public void AddKill(int baseScore, float accuracyBonus)
        {
            if (baseScore <= 0) return;

            float multiplier = comboTracker != null ? comboTracker.ComboMultiplier : 1f;
            float bonusFactor = 1f + Mathf.Max(0f, accuracyBonus);
            int delta = Mathf.RoundToInt(baseScore * bonusFactor * multiplier);

            _score += delta;
            killsThisRun++;

            if (_score > _highScore)
            {
                _highScore = _score;
                PlayerPrefs.SetInt(HighScorePrefKey, _highScore);
                PlayerPrefs.Save();
            }

            ScoreChanged?.Invoke(_score);
        }

        /// <summary>Reset run counters. Does not clear the persisted high score.</summary>
        public void ResetRun()
        {
            _score = 0;
            killsThisRun = 0;
            ScoreChanged?.Invoke(_score);
        }

        /// <summary>Wipe the persisted high score. Used by debug menus / settings.</summary>
        public void ClearHighScore()
        {
            _highScore = 0;
            PlayerPrefs.DeleteKey(HighScorePrefKey);
            PlayerPrefs.Save();
        }

        private void OnZombieKilled(GameEvents.ZombieKilled evt)
        {
            AddKill(evt.scoreAwarded, 0f);
        }

        private void OnPlayerDied(GameEvents.PlayerDied _)
        {
            PlayerPrefs.Save();
        }
    }
}
