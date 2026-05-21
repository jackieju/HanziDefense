using System;
using System.Collections.Generic;
using UnityEngine;
using HanziZombieDefense.Hanzi.Recognition;

namespace HanziZombieDefense.Hanzi.Data
{
    /// <summary>
    /// Single stroke as a polyline of median (skeleton) points in normalized
    /// HanziWriter coordinate space (1024×1024, Y-up after import conversion).
    /// </summary>
    public sealed class HanziStroke
    {
        /// <summary>HanziWriter cell side length. Used as the cell-size hint for stroke classification.</summary>
        public const float HanziCellSize = 1024f;

        private static readonly float HanziCellDiagonal = HanziCellSize * Mathf.Sqrt(2f);

        /// <summary>
        /// Ordered list of points along the stroke median. The first entry is
        /// the stroke start, the last entry is the stroke end.
        /// </summary>
        public IReadOnlyList<Vector2> Points { get; }

        /// <summary>Number of median points. Always ≥ 2 for a valid stroke.</summary>
        public int Count => Points.Count;

        /// <summary>
        /// Canonical stroke type pre-computed from <see cref="Points"/> at
        /// construction. Used by the recognizer to compare against the
        /// player-drawn classification.
        /// </summary>
        public StrokeType Type { get; }

        /// <summary>
        /// Construct a stroke from its median polyline.
        /// </summary>
        /// <param name="points">Ordered median points. Must contain at least two points.</param>
        public HanziStroke(List<Vector2> points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (points.Count < 2)
                throw new ArgumentException("Stroke must have at least 2 points.", nameof(points));

            Points = points.AsReadOnly();

            var typeForward = StrokeClassifier.Classify(points, HanziCellDiagonal);

            var reversed = new List<Vector2>(points);
            reversed.Reverse();
            var typeReversed = StrokeClassifier.Classify(reversed, HanziCellDiagonal);

            Debug.Log($"[HanziStroke] forward={typeForward}, reversed={typeReversed}, points={points.Count}, start={points[0]}, end={points[points.Count-1]}");

            if (typeForward != StrokeType.Unknown && typeReversed == StrokeType.Unknown)
                Type = typeForward;
            else if (typeReversed != StrokeType.Unknown && typeForward == StrokeType.Unknown)
                Type = typeReversed;
            else if (typeForward != StrokeType.Unknown)
                Type = typeForward;
            else
                Type = StrokeType.Unknown;
        }
    }
}
