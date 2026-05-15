using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.Hanzi.Data;

namespace HanziZombieDefense.Hanzi.Recognition
{
    /// <summary>
    /// Default stroke matcher. Compares the player's polyline against a template
    /// stroke using three complementary metrics in canonical (resampled, centered,
    /// unit-box) space:
    ///
    ///   1. <b>Direction</b>: cosine between the two overall headings (last − first).
    ///      Gates wrong-way strokes (writing → instead of ← for a horizontal).
    ///   2. <b>Shape</b>: mean per-index Euclidean distance over the resampled arrays.
    ///      Captures overall path similarity once direction is correct.
    ///   3. <b>Endpoints</b>: |start_a − start_b| + |end_a − end_b|. Catches strokes
    ///      whose interior matches but whose entry/exit positions are wrong.
    ///
    /// All three must clear their thresholds for <see cref="RecognitionResult.IsMatch"/>
    /// to be true. Confidence is computed by linearly mapping each metric into [0, 1]
    /// and combining them with a weighted average — primarily for telemetry.
    /// </summary>
    public sealed class ResampledPointMatcher : IStrokeMatcher
    {
        /// <summary>Default per-metric thresholds, tuned for 32-sample unit-box space.</summary>
        public const float DefaultShapeThreshold = 0.35f;
        public const float DefaultEndpointThreshold = 0.4f;
        public const float DefaultDirectionThreshold = 0.0f;

        private readonly float _shapeThreshold;
        private readonly float _endpointThreshold;
        private readonly float _directionThreshold;
        private readonly int _sampleCount;

        public ResampledPointMatcher(
            float shapeThreshold = DefaultShapeThreshold,
            float endpointThreshold = DefaultEndpointThreshold,
            float directionThreshold = DefaultDirectionThreshold,
            int sampleCount = StrokeNormalizer.DefaultSampleCount)
        {
            _shapeThreshold = shapeThreshold;
            _endpointThreshold = endpointThreshold;
            _directionThreshold = directionThreshold;
            _sampleCount = sampleCount;
        }

        /// <inheritdoc/>
        public RecognitionResult Match(List<Vector2> drawnPoints, HanziStroke expected)
        {
            if (drawnPoints == null || drawnPoints.Count < 2 || expected == null || expected.Count < 2)
                return RecognitionResult.Reject();

            var expectedPoints = new List<Vector2>(expected.Count);
            for (int i = 0; i < expected.Count; i++) expectedPoints.Add(expected.Points[i]);

            var drawnNorm = StrokeNormalizer.Normalize(drawnPoints, _sampleCount);
            var templateNorm = StrokeNormalizer.Normalize(expectedPoints, _sampleCount);

            if (drawnNorm.Count != templateNorm.Count)
                return RecognitionResult.Reject();

            float directionScore = ComputeDirectionScore(drawnNorm, templateNorm);
            float shapeDistance = ComputeShapeDistance(drawnNorm, templateNorm);
            float endpointDistance = ComputeEndpointDistance(drawnNorm, templateNorm);

            bool directionPass = directionScore > _directionThreshold;
            bool shapePass = shapeDistance < _shapeThreshold;
            bool endpointPass = endpointDistance < _endpointThreshold;
            bool isMatch = directionPass && shapePass && endpointPass;

            float confidence = ComputeConfidence(shapeDistance, directionScore, endpointDistance);

            return new RecognitionResult(
                isMatch,
                shapeDistance,
                directionScore,
                endpointDistance,
                confidence);
        }

        /// <summary>
        /// Cosine similarity between the two overall heading vectors (last − first).
        /// Returns −1 when either heading is degenerate (zero length).
        /// </summary>
        private static float ComputeDirectionScore(List<Vector2> a, List<Vector2> b)
        {
            Vector2 va = a[a.Count - 1] - a[0];
            Vector2 vb = b[b.Count - 1] - b[0];

            float la = va.magnitude;
            float lb = vb.magnitude;
            if (la <= float.Epsilon || lb <= float.Epsilon) return -1f;

            return Vector2.Dot(va / la, vb / lb);
        }

        /// <summary>Mean Euclidean distance over corresponding resampled indices.</summary>
        private static float ComputeShapeDistance(List<Vector2> a, List<Vector2> b)
        {
            float sum = 0f;
            int n = a.Count;
            for (int i = 0; i < n; i++) sum += Vector2.Distance(a[i], b[i]);
            return sum / n;
        }

        /// <summary>Sum of start-to-start and end-to-end distances.</summary>
        private static float ComputeEndpointDistance(List<Vector2> a, List<Vector2> b)
        {
            float startD = Vector2.Distance(a[0], b[0]);
            float endD = Vector2.Distance(a[a.Count - 1], b[b.Count - 1]);
            return startD + endD;
        }

        /// <summary>
        /// Combine the three metrics into a 0…1 confidence. Weights chosen so shape
        /// dominates (0.5), endpoints contribute (0.3), and direction adds the final
        /// 0.2. Each metric is mapped to its own [0, 1] window using its threshold.
        /// </summary>
        private float ComputeConfidence(float shape, float direction, float endpoints)
        {
            float shapeScore = 1f - Mathf.Clamp01(shape / Mathf.Max(_shapeThreshold, 1e-4f));
            float endpointScore = 1f - Mathf.Clamp01(endpoints / Mathf.Max(_endpointThreshold, 1e-4f));
            float directionScore = Mathf.Clamp01((direction + 1f) * 0.5f);

            return Mathf.Clamp01(0.5f * shapeScore + 0.3f * endpointScore + 0.2f * directionScore);
        }
    }
}
