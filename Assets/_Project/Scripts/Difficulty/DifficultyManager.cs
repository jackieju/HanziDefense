using UnityEngine;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.Difficulty
{
    /// <summary>
    /// Evaluates <see cref="DifficultyCurve"/> against elapsed gameplay time and exposes
    /// the resulting multipliers/quotas to spawners, AI, and UI. Registers itself in
    /// <see cref="ServiceLocator"/> so any system can resolve it without scene refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DifficultyManager : MonoBehaviour
    {
        [Header("Curve")]
        [SerializeField] private DifficultyCurve curve;

        [Header("Inline overrides (used when no DifficultyCurve asset is assigned)")]
        [SerializeField, Min(1f)] private float fallbackGameDuration = 600f;
        [SerializeField, Min(0.05f)] private float fallbackBaseSpawnInterval = 2f;
        [SerializeField, Min(0.1f)] private float fallbackBaseSpeed = 3f;
        [SerializeField] private AnimationCurve fallbackSpawnRate = AnimationCurve.Linear(0f, 1f, 1f, 3f);
        [SerializeField] private AnimationCurve fallbackSpeed = AnimationCurve.Linear(0f, 1f, 1f, 1.6f);
        [SerializeField] private AnimationCurve fallbackMaxConcurrent = AnimationCurve.Linear(0f, 5f, 1f, 25f);
        [SerializeField] private AnimationCurve fallbackComplexity = AnimationCurve.Linear(0f, 1f, 1f, 5f);

        [Header("HSK Mapping")]
        [SerializeField, Tooltip("Complexity curve output is clamped to [1, this] when interpreted as HSK level.")]
        private int maxHskLevel = 6;

        [SerializeField, Tooltip("Stroke-count budget = baseStrokes + ceil(complexity * strokesPerComplexityUnit).")]
        private int baseMaxStrokes = 4;

        [SerializeField, Min(0)] private int strokesPerComplexityUnit = 2;

        private float _startTime;
        private bool _running;

        public float ElapsedSeconds => _running ? Time.time - _startTime : 0f;

        public float NormalizedTime
        {
            get
            {
                float duration = curve != null ? curve.GameDuration : fallbackGameDuration;
                if (duration <= 0f) return 0f;
                return Mathf.Clamp01(ElapsedSeconds / duration);
            }
        }

        /// <summary>Multiplier applied to spawn rate; >1 means more zombies per second.</summary>
        public float SpawnRateMultiplier => Sample(curve != null ? curve.SpawnRateOverTime : fallbackSpawnRate);

        /// <summary>Multiplier applied to per-zombie movement speed.</summary>
        public float SpeedMultiplier => Sample(curve != null ? curve.SpeedMultiplierOverTime : fallbackSpeed);

        /// <summary>Hard cap on simultaneously-alive zombies for the current moment.</summary>
        public int MaxConcurrentZombies =>
            Mathf.Max(1, Mathf.RoundToInt(Sample(curve != null ? curve.MaxConcurrentOverTime : fallbackMaxConcurrent)));

        /// <summary>HSK level (1..MaxHskLevel) appropriate for the current difficulty point.</summary>
        public int CurrentHSKLevel
        {
            get
            {
                float complexity = Sample(curve != null ? curve.ComplexityOverTime : fallbackComplexity);
                return Mathf.Clamp(Mathf.RoundToInt(complexity), 1, Mathf.Max(1, maxHskLevel));
            }
        }

        /// <summary>Inclusive upper bound on stroke count for newly spawned characters.</summary>
        public int MaxStrokeCount
        {
            get
            {
                float complexity = Sample(curve != null ? curve.ComplexityOverTime : fallbackComplexity);
                return baseMaxStrokes + Mathf.CeilToInt(complexity * strokesPerComplexityUnit);
            }
        }

        /// <summary>Effective spawn interval (seconds) = base / multiplier, floored at 0.05s.</summary>
        public float EffectiveSpawnInterval
        {
            get
            {
                float baseInterval = curve != null ? curve.BaseSpawnInterval : fallbackBaseSpawnInterval;
                float mul = Mathf.Max(0.01f, SpawnRateMultiplier);
                return Mathf.Max(0.05f, baseInterval / mul);
            }
        }

        /// <summary>Effective per-zombie speed = base * multiplier.</summary>
        public float EffectiveZombieSpeed
        {
            get
            {
                float baseSpeed = curve != null ? curve.BaseSpeed : fallbackBaseSpeed;
                return baseSpeed * Mathf.Max(0.05f, SpeedMultiplier);
            }
        }

        private void Awake()
        {
            ServiceLocator.Register(this);
            _startTime = Time.time;
            _running = true;
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<DifficultyManager>(out var registered) && registered == this)
            {
                ServiceLocator.Unregister<DifficultyManager>();
            }
        }

        /// <summary>Reset elapsed timer (e.g. on retry).</summary>
        public void RestartTimer()
        {
            _startTime = Time.time;
            _running = true;
        }

        /// <summary>Pause the elapsed timer without losing accumulated time would require freeze; we just stop progression.</summary>
        public void StopTimer() => _running = false;

        private float Sample(AnimationCurve c)
        {
            if (c == null || c.length == 0) return 1f;
            return c.Evaluate(NormalizedTime);
        }
    }
}
