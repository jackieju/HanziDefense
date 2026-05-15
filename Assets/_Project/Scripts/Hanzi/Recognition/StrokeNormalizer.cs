using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.Hanzi.Recognition
{
    /// <summary>
    /// Geometric helpers that convert raw input polylines into a canonical form
    /// suitable for direct point-by-point comparison:
    ///   1. Arc-length resample to a fixed point count (default 32).
    ///   2. Translate centroid to origin.
    ///   3. Scale to fit a unit-side bounding box, preserving aspect ratio.
    ///
    /// The canonical form is invariant under translation and uniform scale, so
    /// a player drawing the same shape large or small in different parts of the
    /// canvas yields nearly identical normalized polylines.
    /// </summary>
    public static class StrokeNormalizer
    {
        /// <summary>Default sample count used by <see cref="Normalize"/> and <see cref="ResampleToN"/>.</summary>
        public const int DefaultSampleCount = 32;

        /// <summary>
        /// Full normalization pipeline: <c>resample → translate → scale</c>.
        /// Returns a brand-new list. Input is not mutated.
        /// </summary>
        public static List<Vector2> Normalize(List<Vector2> points, int sampleCount = DefaultSampleCount)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 2) return new List<Vector2>(points);

            var resampled = ResampleToN(points, sampleCount);
            var centered = TranslateToOrigin(resampled);
            var scaled = ScaleToUnitBox(centered);
            return scaled;
        }

        /// <summary>
        /// Arc-length resampling — produces exactly <paramref name="n"/> points
        /// evenly spaced along the polyline by *traveled distance* (not by index).
        /// This decouples sampling density from the human input rate.
        /// </summary>
        /// <remarks>
        /// Algorithm (Wobbrock et al., $1 Recognizer):
        ///   • Compute total path length L.
        ///   • Step size I = L / (n - 1).
        ///   • Walk the polyline accumulating segment length d. When d crosses
        ///     I, emit an interpolated point and reset accumulator (carrying
        ///     the residual via segment splitting).
        /// </remarks>
        public static List<Vector2> ResampleToN(List<Vector2> points, int n = DefaultSampleCount)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (n < 2) throw new ArgumentOutOfRangeException(nameof(n), "Sample count must be ≥ 2.");
            if (points.Count == 0) return new List<Vector2>();
            if (points.Count == 1)
            {
                var single = new List<Vector2>(n);
                for (int i = 0; i < n; i++) single.Add(points[0]);
                return single;
            }

            // Work on a mutable copy so we can split segments in-place.
            var src = new List<Vector2>(points);

            float pathLength = 0f;
            for (int i = 1; i < src.Count; i++)
                pathLength += Vector2.Distance(src[i - 1], src[i]);

            // Degenerate input (all points coincident): return n copies of point 0.
            if (pathLength <= float.Epsilon)
            {
                var same = new List<Vector2>(n);
                for (int i = 0; i < n; i++) same.Add(src[0]);
                return same;
            }

            float step = pathLength / (n - 1);
            var output = new List<Vector2>(n) { src[0] };
            float accumulated = 0f;

            for (int i = 1; i < src.Count; i++)
            {
                var prev = src[i - 1];
                var curr = src[i];
                float d = Vector2.Distance(prev, curr);

                if (accumulated + d >= step && d > 0f)
                {
                    float t = (step - accumulated) / d;
                    var q = new Vector2(
                        prev.x + t * (curr.x - prev.x),
                        prev.y + t * (curr.y - prev.y));
                    output.Add(q);
                    src.Insert(i, q);
                    accumulated = 0f;
                }
                else
                {
                    accumulated += d;
                }
            }

            // Floating-point drift can leave us one short — pad with the final point.
            while (output.Count < n) output.Add(src[src.Count - 1]);
            // Or one over due to a borderline last step — clip back.
            if (output.Count > n) output.RemoveRange(n, output.Count - n);

            return output;
        }

        /// <summary>Subtract the centroid from every point. Allocates a new list.</summary>
        public static List<Vector2> TranslateToOrigin(List<Vector2> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count == 0) return new List<Vector2>();

            float sx = 0f, sy = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                sx += points[i].x;
                sy += points[i].y;
            }
            float cx = sx / points.Count;
            float cy = sy / points.Count;

            var result = new List<Vector2>(points.Count);
            for (int i = 0; i < points.Count; i++)
                result.Add(new Vector2(points[i].x - cx, points[i].y - cy));
            return result;
        }

        /// <summary>
        /// Scale uniformly so the longest bounding-box side becomes 1. Preserves
        /// aspect ratio (a horizontal stroke stays horizontal, etc.). Allocates.
        /// </summary>
        public static List<Vector2> ScaleToUnitBox(List<Vector2> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count == 0) return new List<Vector2>();

            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            float w = maxX - minX;
            float h = maxY - minY;
            float longest = Mathf.Max(w, h);

            if (longest <= float.Epsilon)
            {
                // All points coincident — return zeros to keep downstream math sane.
                var zeros = new List<Vector2>(points.Count);
                for (int i = 0; i < points.Count; i++) zeros.Add(Vector2.zero);
                return zeros;
            }

            float invScale = 1f / longest;
            var result = new List<Vector2>(points.Count);
            for (int i = 0; i < points.Count; i++)
                result.Add(points[i] * invScale);
            return result;
        }
    }
}
