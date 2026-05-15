using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.Hanzi.Data
{
    /// <summary>
    /// Single stroke as a polyline of median (skeleton) points in normalized
    /// HanziWriter coordinate space (1024×1024, Y-up after import conversion).
    /// </summary>
    public sealed class HanziStroke
    {
        /// <summary>
        /// Ordered list of points along the stroke median. The first entry is
        /// the stroke start, the last entry is the stroke end.
        /// </summary>
        public IReadOnlyList<Vector2> Points { get; }

        /// <summary>Number of median points. Always ≥ 2 for a valid stroke.</summary>
        public int Count => Points.Count;

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
        }
    }
}
