using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.Hanzi.Data;

namespace HanziZombieDefense.Hanzi.Recognition
{
    /// <summary>
    /// Type-based stroke matcher. Classifies the player's polyline into a
    /// <see cref="StrokeType"/> (横, 竖, 撇, 捺, …) and compares it to the
    /// pre-computed type of the expected stroke. Acceptance is lenient:
    ///
    ///   • Same type or same family (e.g. 横 vs 横钩) → accept.
    ///   • Drawn classifies as <see cref="StrokeType.Unknown"/> but its overall
    ///     heading agrees with the template's → accept.
    ///   • Otherwise reject.
    ///
    /// Shape / endpoint / direction scalars are still computed in canonical
    /// resampled space and reported through <see cref="RecognitionResult"/> so
    /// telemetry and debug HUDs continue to work. They no longer gate the
    /// match — only the type comparison does.
    /// </summary>
    public sealed class ResampledPointMatcher : IStrokeMatcher
    {
        /// <summary>
        /// Default shape-distance threshold. Retained for API / inspector
        /// compatibility — used now only to scale the shape contribution to
        /// the confidence score, not to gate matching.
        /// </summary>
        public const float DefaultShapeThreshold = 0.75f;

        /// <summary>
        /// Default endpoint-distance threshold. Retained for API / inspector
        /// compatibility — used only to scale the endpoint contribution to
        /// the confidence score.
        /// </summary>
        public const float DefaultEndpointThreshold = 0.85f;

        /// <summary>
        /// Cosine threshold used by the heading-agreement fallback when the
        /// drawn stroke could not be classified into a known type.
        /// </summary>
        public const float DefaultDirectionThreshold = 0.5f;

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

            StrokeType expectedType = expected.Type;

            float drawnCellDiag = EstimateDrawnCellDiagonal(drawnPoints);
            StrokeType drawnType = StrokeClassifier.Classify(drawnPoints, out Dir8 drawnPrimary, drawnCellDiag);

            var expectedPointsList = new List<Vector2>(expected.Count);
            for (int i = 0; i < expected.Count; i++) expectedPointsList.Add(expected.Points[i]);
            Dir8 expectedPrimary = StrokeClassifier.GetPrimaryDirection(expectedPointsList);

            var drawnNorm = StrokeNormalizer.Normalize(drawnPoints, _sampleCount);
            var templateNorm = StrokeNormalizer.Normalize(expectedPointsList, _sampleCount);

            if (drawnNorm.Count != templateNorm.Count)
                return RecognitionResult.Reject();

            return Evaluate(drawnNorm, templateNorm, drawnType, expectedType, drawnPrimary, expectedPrimary);
        }

        private RecognitionResult Evaluate(
            List<Vector2> drawnNorm,
            List<Vector2> templateNorm,
            StrokeType drawnType,
            StrokeType expectedType,
            Dir8 drawnPrimary,
            Dir8 expectedPrimary)
        {
            float directionScore = ComputeDirectionScore(drawnNorm, templateNorm);
            float shapeDistance = ComputeShapeDistance(drawnNorm, templateNorm);
            float endpointDistance = ComputeEndpointDistance(drawnNorm, templateNorm);

            bool typesAgree = StrokeClassifier.IsSameFamily(drawnType, expectedType);
            bool directionAgrees = directionScore >= _directionThreshold
                                   && DirectionsCompatible(drawnPrimary, expectedPrimary);

            bool lastSegmentMatch = !typesAgree && StrokeClassifier.IsLastSegmentLengthVariant(drawnNorm, expectedType, templateNorm);

            Debug.Log($"[StrokeMatch] drawn={drawnType}({drawnPrimary}) vs expected={expectedType}({expectedPrimary}) | family={typesAgree} lastSeg={lastSegmentMatch} dirAgree={directionAgrees}");

            bool isMatch;
            if (typesAgree)
            {
                isMatch = true;
            }
            else if (lastSegmentMatch)
            {
                isMatch = true;
            }
            else if (drawnType == StrokeType.Unknown && directionAgrees)
            {
                isMatch = true;
            }
            else if (expectedType == StrokeType.Unknown)
            {
                var drawn16 = StrokeNormalizer.Normalize(new List<Vector2>(drawnNorm), 16);
                var template16 = StrokeNormalizer.Normalize(new List<Vector2>(templateNorm), 16);
                var template16Rev = new List<Vector2>(template16);
                template16Rev.Reverse();

                float shapeFwd = ComputeShapeDistance(drawn16, template16);
                float shapeRev = ComputeShapeDistance(drawn16, template16Rev);
                float bestShape = Mathf.Min(shapeFwd, shapeRev);
                isMatch = bestShape < 0.35f;
            }
            else
            {
                isMatch = false;
            }

            float confidence = ComputeConfidence(isMatch, typesAgree, directionScore, shapeDistance, endpointDistance);

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
            float startD = Vector2.Distance(a[0], b[0]);
            float endD = Vector2.Distance(a[a.Count - 1], b[b.Count - 1]);
            return startD + endD;
        }

        private static bool DirectionsCompatible(Dir8 a, Dir8 b)
        {
            int diff = Mathf.Abs((int)a - (int)b) % 8;
            if (diff > 4) diff = 8 - diff;
            return diff <= 1;
        }

        private static Dir8 OppositeDirection(Dir8 d) => (Dir8)(((int)d + 4) % 8);

        private float ComputeConfidence(bool isMatch, bool typesAgree, float direction, float shape, float endpoints)
        {
            float typeScore = typesAgree ? 1f : 0f;
            float directionScore = Mathf.Clamp01((direction + 1f) * 0.5f);
            float shapeScore = 1f - Mathf.Clamp01(shape / Mathf.Max(_shapeThreshold, 1e-4f));
            float endpointScore = 1f - Mathf.Clamp01(endpoints / Mathf.Max(_endpointThreshold, 1e-4f));

            float blended = 0.5f * typeScore + 0.2f * directionScore + 0.2f * shapeScore + 0.1f * endpointScore;
            return isMatch ? Mathf.Max(blended, 0.5f) : Mathf.Min(blended, 0.49f);
        }

        private static float EstimateDrawnCellDiagonal(List<Vector2> points)
        {
            if (points == null || points.Count < 2) return 0f;
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].x < minX) minX = points[i].x;
                if (points[i].x > maxX) maxX = points[i].x;
                if (points[i].y < minY) minY = points[i].y;
                if (points[i].y > maxY) maxY = points[i].y;
            }
            float strokeDiag = Mathf.Sqrt((maxX - minX) * (maxX - minX) + (maxY - minY) * (maxY - minY));
            return strokeDiag * 6f;
        }
    }
}
