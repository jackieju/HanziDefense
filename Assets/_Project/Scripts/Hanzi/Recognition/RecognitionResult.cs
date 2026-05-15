namespace HanziZombieDefense.Hanzi.Recognition
{
    /// <summary>
    /// Outcome of a single stroke comparison. Carries both the binary verdict
    /// and the underlying scalar metrics so debugging UI / telemetry / dynamic
    /// difficulty can introspect why a stroke passed or failed.
    /// </summary>
    public sealed class RecognitionResult
    {
        /// <summary>True iff every metric passed its threshold.</summary>
        public bool IsMatch { get; }

        /// <summary>Mean Euclidean distance between corresponding resampled points (unit-box space).</summary>
        public float ShapeDistance { get; }

        /// <summary>Dot product of normalized overall direction vectors. Range −1…1; positive means same heading.</summary>
        public float DirectionScore { get; }

        /// <summary>Sum of start-point and end-point distances (unit-box space).</summary>
        public float EndpointDistance { get; }

        /// <summary>Aggregate confidence in [0, 1]. 1 = perfect match.</summary>
        public float Confidence { get; }

        public RecognitionResult(
            bool isMatch,
            float shapeDistance,
            float directionScore,
            float endpointDistance,
            float confidence)
        {
            IsMatch = isMatch;
            ShapeDistance = shapeDistance;
            DirectionScore = directionScore;
            EndpointDistance = endpointDistance;
            Confidence = confidence;
        }

        /// <summary>Convenience factory for the "stroke had &lt; 2 points" failure path.</summary>
        public static RecognitionResult Reject() =>
            new RecognitionResult(false, float.PositiveInfinity, -1f, float.PositiveInfinity, 0f);

        public override string ToString() =>
            $"Match={IsMatch} shape={ShapeDistance:F3} dir={DirectionScore:F2} " +
            $"endpoints={EndpointDistance:F3} conf={Confidence:F2}";
    }
}
