using UnityEngine;

namespace HanziZombieDefense.Difficulty
{
    /// <summary>
    /// Authoring asset bundling all difficulty curves for one play session,
    /// plus baseline values they multiply against.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DifficultyCurve",
        menuName = "HanziZombieDefense/Difficulty Curve",
        order = 200)]
    public sealed class DifficultyCurve : ScriptableObject
    {
        [Header("Time Domain")]
        [SerializeField, Min(1f), Tooltip("Total game length used to normalize time → curve t in [0,1]. Default 600s = 10 min.")]
        private float gameDuration = 600f;

        [Header("Baselines")]
        [SerializeField, Min(0.05f), Tooltip("Spawn interval (seconds) at curve value 1.0.")]
        private float baseSpawnInterval = 2f;

        [SerializeField, Min(0.1f), Tooltip("Zombie movement speed (m/s) at speed multiplier 1.0.")]
        private float baseSpeed = 3f;

        [Header("Curves (X = normalized elapsed time 0..1)")]
        [SerializeField, Tooltip("Multiplies spawn rate. >1 = faster spawns. Default rises from 1 → 3 over the session.")]
        private AnimationCurve spawnRateOverTime = AnimationCurve.Linear(0f, 1f, 1f, 3f);

        [SerializeField, Tooltip("Multiplies zombie speed. >1 = faster.")]
        private AnimationCurve speedMultiplierOverTime = AnimationCurve.Linear(0f, 1f, 1f, 1.6f);

        [SerializeField, Tooltip("Hard cap on simultaneously alive zombies.")]
        private AnimationCurve maxConcurrentOverTime = AnimationCurve.Linear(0f, 5f, 1f, 25f);

        [SerializeField, Tooltip("Drives stroke-count budget / HSK level. Output range typically 1..6.")]
        private AnimationCurve complexityOverTime = AnimationCurve.Linear(0f, 1f, 1f, 5f);

        public float GameDuration => gameDuration;
        public float BaseSpawnInterval => baseSpawnInterval;
        public float BaseSpeed => baseSpeed;

        public AnimationCurve SpawnRateOverTime => spawnRateOverTime;
        public AnimationCurve SpeedMultiplierOverTime => speedMultiplierOverTime;
        public AnimationCurve MaxConcurrentOverTime => maxConcurrentOverTime;
        public AnimationCurve ComplexityOverTime => complexityOverTime;

        /// <summary>Map an absolute elapsed time (s) to a normalized curve t ∈ [0,1].</summary>
        public float NormalizeTime(float elapsedSeconds)
        {
            if (gameDuration <= 0f) return 0f;
            return Mathf.Clamp01(elapsedSeconds / gameDuration);
        }
    }
}
