using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.Hanzi.Data;

namespace HanziZombieDefense.Hanzi.Recognition
{
    public sealed class ShapePointMatcher : IStrokeMatcher
    {
        public const float DefaultShapeThreshold = 0.55f;
        public const float DefaultEndpointThreshold = 0.65f;
        public const float DefaultDirectionThreshold = -0.7f;

        private readonly float _shapeThreshold;
        private readonly float _endpointThreshold;
        private readonly float _directionThreshold;
        private readonly int _sampleCount;

        public ShapePointMatcher(
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

            var resultForward = Evaluate(drawnNorm, templateNorm);

            var templateReversed = new List<Vector2>(templateNorm);
            templateReversed.Reverse();
            var resultReversed = Evaluate(drawnNorm, templateReversed);

            return resultForward.Confidence >= resultReversed.Confidence ? resultForward : resultReversed;
        }

        private RecognitionResult Evaluate(List<Vector2> drawnNorm, List<Vector2> templateNorm)
        {
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

        private static float ComputeDirectionScore(List<Vector2> a, List<Vector2> b)
        {
            Vector2 va = a[a.Count - 1] - a[0];
            Vector2 vb = b[b.Count - 1] - b[0];
            float la = va.magnitude;
            float lb = vb.magnitude;
            if (la <= float.Epsilon || lb <= float.Epsilon) return -1f;
            return Vector2.Dot(va / la, vb / lb);
        }

        private static float ComputeShapeDistance(List<Vector2> a, List<Vector2> b)
        {
            float sum = 0f;
            int n = a.Count;
            for (int i = 0; i < n; i++) sum += Vector2.Distance(a[i], b[i]);
            return sum / n;
        }

        private static float ComputeEndpointDistance(List<Vector2> a, List<Vector2> b)
        {
            return Vector2.Distance(a[0], b[0]) + Vector2.Distance(a[a.Count - 1], b[b.Count - 1]);
        }

        private float ComputeConfidence(float shape, float direction, float endpoints)
        {
            float shapeScore = 1f - Mathf.Clamp01(shape / Mathf.Max(_shapeThreshold, 1e-4f));
            float endpointScore = 1f - Mathf.Clamp01(endpoints / Mathf.Max(_endpointThreshold, 1e-4f));
            float directionScore = Mathf.Clamp01((direction + 1f) * 0.5f);
            return Mathf.Clamp01(0.5f * shapeScore + 0.3f * endpointScore + 0.2f * directionScore);
        }
    }
}
