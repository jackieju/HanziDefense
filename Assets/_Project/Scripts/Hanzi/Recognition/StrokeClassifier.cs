using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.Hanzi.Recognition
{
    /// <summary>
    /// Classifies a polyline (template median or player-drawn) into a
    /// <see cref="StrokeType"/> by analyzing its direction sequence.
    ///
    /// Pipeline:
    ///   1. Compute the bbox of the raw input. If a <c>cellDiagonal</c> hint is
    ///      provided and the stroke's bbox diagonal is below
    ///      <see cref="ShortStrokeFraction"/> of the cell, classify as
    ///      <see cref="StrokeType.Dian"/> / <see cref="StrokeType.Ti"/>.
    ///   2. Normalize the polyline to a unit-side bbox so all subsequent
    ///      thresholds are scale-free.
    ///   3. RDP-simplify with epsilon = <see cref="RdpEpsilonFraction"/> ×
    ///      bbox-diag of the normalized polyline.
    ///   4. Build a list of (Dir8, length) segments from the simplified path
    ///      and merge runs of identical directions that survived simplification.
    ///   5. Detect a hook on the final segment (short tail with sharp turn).
    ///   6. Pattern-match the resulting macro-direction sequence to a
    ///      <see cref="StrokeType"/>.
    ///
    /// The classifier is intentionally permissive: messy player strokes that
    /// don't match any known pattern fall through to <see cref="StrokeType.Unknown"/>,
    /// which the matcher then resolves via direction agreement instead.
    /// </summary>
    public static class StrokeClassifier
    {
        /// <summary>Strokes whose raw bbox diag is below this fraction of the cell are treated as 点/提.</summary>
        public const float ShortStrokeFraction = 0.15f;

        /// <summary>RDP epsilon as a fraction of the normalized polyline's bbox diagonal.</summary>
        public const float RdpEpsilonFraction = 0.08f;

        /// <summary>Octant gap between consecutive segments that qualifies as a hook turn.</summary>
        public const int HookTurnOctants = 2;

        /// <summary>Last-segment length must be below this fraction of total path length to count as a hook.</summary>
        public const float HookLengthFraction = 0.25f;

        private const float TwoPi = Mathf.PI * 2f;

        /// <summary>
        /// Classify <paramref name="points"/> into a <see cref="StrokeType"/>.
        /// </summary>
        /// <param name="points">Polyline points. Need not be resampled or normalized.</param>
        /// <param name="cellDiagonal">
        /// Diagonal length of the character cell in the same units as <paramref name="points"/>.
        /// Pass <c>0</c> (default) to skip the short-stroke heuristic — useful when classifying
        /// player input whose canvas size is unknown to the caller.
        /// </param>
        public static StrokeType Classify(IReadOnlyList<Vector2> points, float cellDiagonal = 0f)
        {
            return Classify(points, out _, cellDiagonal);
        }

        /// <summary>
        /// Classify and report the overall heading direction.
        /// </summary>
        public static StrokeType Classify(IReadOnlyList<Vector2> points, out Dir8 primaryDirection, float cellDiagonal = 0f)
        {
            primaryDirection = Dir8.E;
            if (points == null || points.Count < 2) return StrokeType.Unknown;

            ComputeBoundingBox(points, out Vector2 bMin, out Vector2 bMax);
            Vector2 size = bMax - bMin;
            float rawDiag = size.magnitude;

            if (rawDiag <= float.Epsilon)
                return StrokeType.Dian;

            primaryDirection = QuantizeDirection(points[points.Count - 1] - points[0]);

            if (cellDiagonal > 0f && rawDiag < ShortStrokeFraction * cellDiagonal)
            {
                return primaryDirection == Dir8.NE ? StrokeType.Ti : StrokeType.Dian;
            }

            var normalized = NormalizeToUnitBox(points, bMin, size);

            ComputeBoundingBox(normalized, out Vector2 nMin, out Vector2 nMax);
            float normDiag = (nMax - nMin).magnitude;
            float epsilon = RdpEpsilonFraction * normDiag;

            var simplified = RamerDouglasPeucker(normalized, epsilon);
            if (simplified.Count < 2)
                return primaryDirection == Dir8.NE ? StrokeType.Ti : StrokeType.Dian;

            var segments = BuildSegments(simplified);
            segments = MergeSameDirection(segments);
            if (segments.Count == 0) return StrokeType.Unknown;

            segments = TrimLeadingMinorSegments(segments);
            if (segments.Count == 0) return StrokeType.Unknown;

            primaryDirection = segments[0].Dir;

            bool hasHook = DetectHook(segments);
            return PatternMatch(segments, hasHook, primaryDirection);
        }

        /// <summary>
        /// Compute the overall heading (last − first) quantized to one of eight compass directions.
        /// </summary>
        public static Dir8 GetPrimaryDirection(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 2) return Dir8.E;
            return QuantizeDirection(points[points.Count - 1] - points[0]);
        }

        /// <summary>
        /// Returns true when the drawn stroke has the same segment directions as the
        /// expected type, but the last segment is longer (causing a different classification).
        /// Example: expected=ShuGou, drawn has same S+hook(W) directions but hook is too long
        /// so it classified as ShuZhe instead.
        /// </summary>
        public static bool IsLastSegmentLengthVariant(IReadOnlyList<Vector2> drawnPoints, StrokeType expectedType, IReadOnlyList<Vector2> templatePoints)
        {
            if (expectedType == StrokeType.Unknown) return false;
            if (drawnPoints == null || drawnPoints.Count < 3) return false;
            if (templatePoints == null || templatePoints.Count < 3) return false;

            ComputeBoundingBox(drawnPoints, out Vector2 bMin, out Vector2 bMax);
            Vector2 size = bMax - bMin;
            if (size.magnitude <= float.Epsilon) return false;

            var normalized = NormalizeToUnitBox(drawnPoints, bMin, size);
            if (normalized.Count < 3) return false;

            ComputeBoundingBox(normalized, out Vector2 nMin, out Vector2 nMax);
            float normDiag = (nMax - nMin).magnitude;
            float epsilon = RdpEpsilonFraction * normDiag;

            var simplified = RamerDouglasPeucker(normalized, epsilon);
            if (simplified.Count < 3) return false;

            var segments = BuildSegments(simplified);
            segments = MergeSameDirection(segments);
            if (segments.Count < 2) return false;

            float totalLen = 0f;
            for (int i = 0; i < segments.Count; i++) totalLen += segments[i].Length;
            float lastFraction = segments[segments.Count - 1].Length / totalLen;

            if (lastFraction <= HookLengthFraction) return false;

            Dir8 drawnLastDir = segments[segments.Count - 1].Dir;

            ComputeBoundingBox(templatePoints, out Vector2 tMin, out Vector2 tMax);
            Vector2 tSize = tMax - tMin;
            if (tSize.magnitude <= float.Epsilon) return false;
            var tNorm = NormalizeToUnitBox(templatePoints, tMin, tSize);
            ComputeBoundingBox(tNorm, out Vector2 tnMin, out Vector2 tnMax);
            float tDiag = (tnMax - tnMin).magnitude;
            var tSimplified = RamerDouglasPeucker(tNorm, RdpEpsilonFraction * tDiag);
            if (tSimplified.Count < 2) return false;
            var tSegments = BuildSegments(tSimplified);
            tSegments = MergeSameDirection(tSegments);
            if (tSegments.Count < 2) return false;
            Dir8 templateLastDir = tSegments[tSegments.Count - 1].Dir;

            if (OctantDistance(drawnLastDir, templateLastDir) > 2) return false;

            var shortened = new List<Segment>(segments.Count);
            for (int i = 0; i < segments.Count - 1; i++) shortened.Add(segments[i]);
            var lastVec = segments[segments.Count - 1].Vector.normalized * 0.01f;
            shortened.Add(new Segment(drawnLastDir, 0.01f, lastVec));

            var reclassified = PatternMatch(shortened, true, shortened[0].Dir);
            return IsSameFamily(reclassified, expectedType);
        }

        /// <summary>
        /// Lenient family equivalence. Parameter order matters:
        /// <paramref name="drawn"/> is what the player drew,
        /// <paramref name="expected"/> is what the template requires.
        /// A player who draws MORE detail (e.g. adds a hook) is accepted,
        /// but a player who draws LESS (omits a required hook) is rejected.
        /// </summary>
        public static bool IsSameFamily(StrokeType drawn, StrokeType expected)
        {
            if (drawn == expected) return true;
            if (drawn == StrokeType.Unknown || expected == StrokeType.Unknown) return false;

            return DrawnAcceptsExpected(drawn, expected);
        }

        private static bool DrawnAcceptsExpected(StrokeType drawn, StrokeType expected)
        {
            switch (expected)
            {
                case StrokeType.Heng:
                    return drawn == StrokeType.HengGou;
                case StrokeType.Shu:
                    return drawn == StrokeType.ShuGou || drawn == StrokeType.ShuWanGou || drawn == StrokeType.WanGou;
                case StrokeType.HengZhe:
                    return drawn == StrokeType.HengZheGou || drawn == StrokeType.HengZheWanGou;
                case StrokeType.HengZheGou:
                    return drawn == StrokeType.HengZheWanGou;
                case StrokeType.ShuGou:
                    return drawn == StrokeType.WanGou || drawn == StrokeType.ShuWanGou;
                case StrokeType.WanGou:
                    return drawn == StrokeType.ShuGou || drawn == StrokeType.ShuWanGou;
                case StrokeType.Na:
                    return drawn == StrokeType.Dian || drawn == StrokeType.XieGou || drawn == StrokeType.HengPie || drawn == StrokeType.Ti;
                case StrokeType.Dian:
                    return drawn == StrokeType.Ti || drawn == StrokeType.Na || drawn == StrokeType.Pie;
                case StrokeType.Ti:
                    return drawn == StrokeType.Dian || drawn == StrokeType.Na;
                case StrokeType.Pie:
                    return drawn == StrokeType.PieZhe || drawn == StrokeType.Dian;
                case StrokeType.XieGou:
                    return drawn == StrokeType.Na;
                case StrokeType.HengPie:
                    return drawn == StrokeType.Na || drawn == StrokeType.Pie;
                default:
                    return false;
            }
        }

        private static void ComputeBoundingBox(IReadOnlyList<Vector2> pts, out Vector2 min, out Vector2 max)
        {
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            min = new Vector2(minX, minY);
            max = new Vector2(maxX, maxY);
        }

        private static List<Vector2> NormalizeToUnitBox(IReadOnlyList<Vector2> pts, Vector2 bMin, Vector2 size)
        {
            float scale = 1f / Mathf.Max(Mathf.Max(size.x, size.y), 1e-6f);
            var result = new List<Vector2>(pts.Count);
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                result.Add(new Vector2((p.x - bMin.x) * scale, (p.y - bMin.y) * scale));
            }
            return result;
        }

        private static Dir8 QuantizeDirection(Vector2 v)
        {
            if (v.sqrMagnitude <= float.Epsilon) return Dir8.E;
            float angle = Mathf.Atan2(v.y, v.x);
            if (angle < 0f) angle += TwoPi;
            int octant = Mathf.RoundToInt(angle / (Mathf.PI / 4f)) % 8;
            return (Dir8)octant;
        }

        private static List<Vector2> RamerDouglasPeucker(List<Vector2> points, float epsilon)
        {
            int n = points.Count;
            if (n < 3) return new List<Vector2>(points);

            var keep = new bool[n];
            keep[0] = true;
            keep[n - 1] = true;

            var stack = new Stack<(int, int)>();
            stack.Push((0, n - 1));

            while (stack.Count > 0)
            {
                var (lo, hi) = stack.Pop();
                if (hi <= lo + 1) continue;

                Vector2 a = points[lo];
                Vector2 b = points[hi];
                float maxDist = 0f;
                int maxIdx = -1;

                for (int i = lo + 1; i < hi; i++)
                {
                    float d = PerpendicularLineDistance(points[i], a, b);
                    if (d > maxDist) { maxDist = d; maxIdx = i; }
                }

                if (maxDist > epsilon && maxIdx > 0)
                {
                    keep[maxIdx] = true;
                    stack.Push((lo, maxIdx));
                    stack.Push((maxIdx, hi));
                }
            }

            var result = new List<Vector2>(n);
            for (int i = 0; i < n; i++) if (keep[i]) result.Add(points[i]);
            return result;
        }

        private static float PerpendicularLineDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-12f) return Vector2.Distance(p, a);
            Vector2 ap = p - a;
            float t = Vector2.Dot(ap, ab) / lenSq;
            Vector2 proj = a + ab * t;
            return Vector2.Distance(p, proj);
        }

        private readonly struct Segment
        {
            public readonly Dir8 Dir;
            public readonly float Length;
            public readonly Vector2 Vector;

            public Segment(Dir8 dir, float length, Vector2 vector)
            {
                Dir = dir;
                Length = length;
                Vector = vector;
            }
        }

        private static List<Segment> BuildSegments(List<Vector2> simplified)
        {
            var segs = new List<Segment>(simplified.Count - 1);
            for (int i = 1; i < simplified.Count; i++)
            {
                Vector2 v = simplified[i] - simplified[i - 1];
                float len = v.magnitude;
                if (len <= 1e-6f) continue;
                segs.Add(new Segment(QuantizeDirection(v), len, v));
            }
            return segs;
        }

        private static List<Segment> MergeSameDirection(List<Segment> segs)
        {
            if (segs.Count <= 1) return segs;
            var merged = new List<Segment>(segs.Count);
            Segment cur = segs[0];
            for (int i = 1; i < segs.Count; i++)
            {
                if (segs[i].Dir == cur.Dir)
                {
                    Vector2 combined = cur.Vector + segs[i].Vector;
                    cur = new Segment(cur.Dir, cur.Length + segs[i].Length, combined);
                }
                else
                {
                    merged.Add(cur);
                    cur = segs[i];
                }
            }
            merged.Add(cur);
            return merged;
        }

        private static List<Segment> TrimLeadingMinorSegments(List<Segment> segs)
        {
            if (segs.Count <= 1) return segs;
            return segs;
        }

        private static List<Vector2> TrimOverlappedPrefix(List<Vector2> points)
        {
            if (points.Count < 4) return points;

            int lastIdx = points.Count - 1;
            Vector2 endpoint = points[lastIdx];

            float maxDist = 0f;
            int farthestIdx = 0;
            for (int i = 0; i < points.Count; i++)
            {
                float dist = Vector2.Distance(points[i], endpoint);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    farthestIdx = i;
                }
            }

            if (farthestIdx <= 0) return points;
            return points.GetRange(farthestIdx, points.Count - farthestIdx);
        }

        private static bool DetectHook(List<Segment> segs)
        {
            if (segs.Count < 2) return false;

            float total = 0f;
            for (int i = 0; i < segs.Count; i++) total += segs[i].Length;

            Segment last = segs[segs.Count - 1];
            Segment first = segs[0];

            if (last.Length > HookLengthFraction * total) return false;

            int gap = OctantDistance(first.Dir, last.Dir);
            return gap >= HookTurnOctants;
        }

        private static int OctantDistance(Dir8 a, Dir8 b)
        {
            int diff = Mathf.Abs((int)a - (int)b) % 8;
            return diff > 4 ? 8 - diff : diff;
        }

        private static StrokeType PatternMatch(List<Segment> segs, bool hook, Dir8 primaryDir)
        {
            int total = segs.Count;
            int main = hook ? total - 1 : total;
            Dir8 hookDir = hook ? segs[total - 1].Dir : Dir8.E;

            if (main <= 0) return StrokeType.Unknown;

            if (main == 1)
            {
                Dir8 d = segs[0].Dir;
                if (!hook) return SingleSegment(d, primaryDir);
                StrokeType hookType = SingleSegmentWithHook(d);
                if (hookType == StrokeType.ShuGou && !IsLeftward(hookDir)) return StrokeType.Unknown;
                if (hookType == StrokeType.HengGou && !IsDownward(hookDir)) return StrokeType.Unknown;
                return hookType;
            }

            if (main == 2)
            {
                Dir8 a = segs[0].Dir;
                Dir8 b = segs[1].Dir;
                return hook ? TwoSegmentsWithHook(a, b) : TwoSegments(a, b);
            }

            if (main == 3)
            {
                Dir8 a = segs[0].Dir;
                Dir8 b = segs[1].Dir;
                Dir8 c = segs[2].Dir;
                if (hook)
                {
                    if (IsHorizontal(a) && IsVertical(b) && IsHorizontal(c)) return StrokeType.HengZheWanGou;
                }
                else
                {
                    if (IsHorizontal(a) && IsVertical(b) && IsHorizontal(c)) return StrokeType.HengZhe;
                }
            }

            return StrokeType.Unknown;
        }

        private static StrokeType SingleSegment(Dir8 d, Dir8 primaryDir)
        {
            switch (d)
            {
                case Dir8.E: return StrokeType.Heng;
                case Dir8.W: return StrokeType.Heng;
                case Dir8.S: return StrokeType.Shu;
                case Dir8.N: return StrokeType.Shu;
                case Dir8.SW: return StrokeType.Pie;
                case Dir8.SE: return StrokeType.Na;
                case Dir8.NE: return StrokeType.Ti;
                case Dir8.NW:
                    return primaryDir == Dir8.NW ? StrokeType.Pie : StrokeType.Unknown;
                default: return StrokeType.Unknown;
            }
        }

        private static StrokeType SingleSegmentWithHook(Dir8 d)
        {
            switch (d)
            {
                case Dir8.E: return StrokeType.HengGou;
                case Dir8.W: return StrokeType.HengGou;
                case Dir8.S: return StrokeType.ShuGou;
                case Dir8.N: return StrokeType.ShuGou;
                case Dir8.SE: return StrokeType.XieGou;
                case Dir8.NW: return StrokeType.XieGou;
                default: return StrokeType.Unknown;
            }
        }

        private static StrokeType TwoSegments(Dir8 a, Dir8 b)
        {
            if (IsHorizontal(a) && IsVertical(b)) return StrokeType.HengZhe;
            if (IsVertical(a) && IsHorizontal(b)) return StrokeType.ShuZhe;
            if (IsHorizontal(a) && b == Dir8.SW) return StrokeType.HengPie;
            if (a == Dir8.SW && IsHorizontal(b)) return StrokeType.PieZhe;
            if (a == Dir8.SW && b == Dir8.SE) return StrokeType.PieZhe;
            return StrokeType.Unknown;
        }

        private static StrokeType TwoSegmentsWithHook(Dir8 a, Dir8 b)
        {
            if (IsHorizontal(a) && IsVertical(b)) return StrokeType.HengZheGou;
            if (IsVertical(a) && IsHorizontal(b)) return StrokeType.ShuWanGou;
            if (IsVertical(a) && b == Dir8.SE) return StrokeType.WanGou;
            if (a == Dir8.SE && IsHorizontal(b)) return StrokeType.ShuWanGou;
            if (OctantDistance(a, b) <= 1) return SingleSegmentWithHook(a);
            return StrokeType.Unknown;
        }

        private static bool IsHorizontal(Dir8 d) => d == Dir8.E || d == Dir8.W;
        private static bool IsVertical(Dir8 d) => d == Dir8.S || d == Dir8.N;
        private static bool IsLeftward(Dir8 d) => d == Dir8.W || d == Dir8.NW || d == Dir8.SW;
        private static bool IsDownward(Dir8 d) => d == Dir8.S || d == Dir8.SW || d == Dir8.SE;
    }
}
