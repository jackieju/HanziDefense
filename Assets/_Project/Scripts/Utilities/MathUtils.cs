using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.Utilities
{
    /// <summary>
    /// Stateless 2D math helpers used by stroke recognition (resampling, normalization,
    /// path geometry). All methods are allocation-aware and tolerate degenerate inputs.
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Resample <paramref name="points"/> to exactly <paramref name="n"/> points evenly spaced
        /// along the polyline's arc length. Implementation follows the $1 Recognizer's resampling step:
        /// walk the path placing a new point each time the cumulative distance crosses the target spacing,
        /// linearly interpolating between the surrounding source points.
        /// </summary>
        /// <param name="points">Source polyline. Must have at least 2 points for a non-trivial result.</param>
        /// <param name="n">Target count. Clamped to a minimum of 2.</param>
        /// <returns>New list with exactly <paramref name="n"/> points (or fewer if input is empty/single).</returns>
        public static List<Vector2> ResamplePoints(List<Vector2> points, int n)
        {
            if (points == null || points.Count == 0)
            {
                return new List<Vector2>();
            }
            if (points.Count == 1 || n <= 1)
            {
                return new List<Vector2> { points[0] };
            }

            n = Mathf.Max(n, 2);

            float totalLength = PathLength(points);
            if (totalLength <= Mathf.Epsilon)
            {
                var flat = new List<Vector2>(n);
                for (int i = 0; i < n; i++) flat.Add(points[0]);
                return flat;
            }

            float interval = totalLength / (n - 1);
            float accumulated = 0f;

            var result = new List<Vector2>(n) { points[0] };

            var working = new List<Vector2>(points.Count);
            working.AddRange(points);

            int index = 1;
            while (index < working.Count)
            {
                Vector2 prev = working[index - 1];
                Vector2 curr = working[index];
                float segLen = Vector2.Distance(prev, curr);

                if (accumulated + segLen >= interval && segLen > Mathf.Epsilon)
                {
                    float t = (interval - accumulated) / segLen;
                    Vector2 sample = new Vector2(
                        prev.x + t * (curr.x - prev.x),
                        prev.y + t * (curr.y - prev.y));

                    result.Add(sample);
                    working.Insert(index, sample);
                    accumulated = 0f;
                }
                else
                {
                    accumulated += segLen;
                }
                index++;
            }

            while (result.Count < n)
            {
                result.Add(working[working.Count - 1]);
            }

            if (result.Count > n)
            {
                result.RemoveRange(n, result.Count - n);
            }

            return result;
        }

        /// <summary>Sum of segment lengths along the polyline. Returns 0 for null / sub-2-point input.</summary>
        public static float PathLength(List<Vector2> points)
        {
            if (points == null || points.Count < 2) return 0f;

            float length = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector2.Distance(points[i - 1], points[i]);
            }
            return length;
        }

        /// <summary>Arithmetic mean of the points. Returns <see cref="Vector2.zero"/> for empty input.</summary>
        public static Vector2 Centroid(List<Vector2> points)
        {
            if (points == null || points.Count == 0) return Vector2.zero;

            float sumX = 0f, sumY = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                sumX += points[i].x;
                sumY += points[i].y;
            }
            float inv = 1f / points.Count;
            return new Vector2(sumX * inv, sumY * inv);
        }

        /// <summary>
        /// Axis-aligned bounding box. Returns (zero, zero) for empty input.
        /// </summary>
        public static (Vector2 min, Vector2 max) BoundingBox(List<Vector2> points)
        {
            if (points == null || points.Count == 0)
            {
                return (Vector2.zero, Vector2.zero);
            }

            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = points[i];
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }

            return (new Vector2(minX, minY), new Vector2(maxX, maxY));
        }

        /// <summary>
        /// Translate the centroid to the origin and uniformly scale so the longest bounding-box
        /// edge fits the [-0.5, 0.5] interval. Aspect ratio is preserved (uniform scale).
        /// </summary>
        public static List<Vector2> NormalizeToUnitBox(List<Vector2> points)
        {
            if (points == null || points.Count == 0) return new List<Vector2>();

            var centroid = Centroid(points);
            var (min, max) = BoundingBox(points);

            float width = max.x - min.x;
            float height = max.y - min.y;
            float maxDim = Mathf.Max(width, height);

            float scale = maxDim > Mathf.Epsilon ? 1f / maxDim : 0f;

            var normalized = new List<Vector2>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = points[i] - centroid;
                normalized.Add(new Vector2(p.x * scale, p.y * scale));
            }
            return normalized;
        }
    }
}
