using System.Collections.Generic;
using UnityEngine;

namespace HanziZombieDefense.Hanzi.Input
{
    /// <summary>
    /// Accumulates raw pointer samples into per-stroke point lists, suitable
    /// for the recognition pipeline. Applies a minimum-distance filter so
    /// dense pointer-move events do not bloat the polyline.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrokeRecorder : MonoBehaviour
    {
        [Tooltip("Minimum pixel distance between consecutive recorded points.")]
        [SerializeField] private float minPointDistance = 2f;

        [Tooltip("Initial capacity reserved per stroke; over-allocation is cheap and avoids resizes.")]
        [SerializeField] private int initialCapacityPerStroke = 128;

        private readonly List<List<Vector2>> _completedStrokes = new List<List<Vector2>>(16);
        private List<Vector2> _current;

        /// <summary>The stroke currently being recorded, or null if no stroke is active.</summary>
        public IReadOnlyList<Vector2> CurrentStroke => _current;

        /// <summary>All strokes finished since the last <see cref="ClearAll"/>.</summary>
        public IReadOnlyList<IReadOnlyList<Vector2>> CompletedStrokes => _completedStrokes;

        /// <summary>True when a stroke is in progress.</summary>
        public bool IsRecording => _current != null;

        /// <summary>
        /// Begin a new stroke at <paramref name="pos"/>. If a stroke was already
        /// in progress it is silently terminated.
        /// </summary>
        public void BeginStroke(Vector2 pos)
        {
            if (_current != null) EndStroke();
            _current = new List<Vector2>(initialCapacityPerStroke) { pos };
        }

        /// <summary>
        /// Append <paramref name="pos"/> to the current stroke if it is at least
        /// <see cref="minPointDistance"/> from the previous point. Returns true
        /// if the point was actually appended.
        /// </summary>
        /// <param name="force">When true, append regardless of the distance filter.</param>
        public bool AddPoint(Vector2 pos, bool force = false)
        {
            if (_current == null) return false;
            if (_current.Count == 0)
            {
                _current.Add(pos);
                return true;
            }

            if (!force)
            {
                var last = _current[_current.Count - 1];
                if (Vector2.SqrMagnitude(pos - last) < minPointDistance * minPointDistance)
                    return false;
            }

            _current.Add(pos);
            return true;
        }

        /// <summary>
        /// Finish the current stroke. Returns the captured point list (or null
        /// if no stroke was active). The list is also appended to
        /// <see cref="CompletedStrokes"/>.
        /// </summary>
        public List<Vector2> EndStroke()
        {
            if (_current == null) return null;

            var stroke = _current;
            _current = null;

            if (stroke.Count >= 2) _completedStrokes.Add(stroke);
            return stroke;
        }

        /// <summary>Drop the in-progress stroke, leaving completed strokes intact.</summary>
        public void DiscardCurrent()
        {
            _current = null;
        }

        /// <summary>Drop the most recent completed stroke (for "reject and retry" UX).</summary>
        public void RemoveLastCompleted()
        {
            if (_completedStrokes.Count == 0) return;
            _completedStrokes.RemoveAt(_completedStrokes.Count - 1);
        }

        /// <summary>Reset all state.</summary>
        public void ClearAll()
        {
            _current = null;
            _completedStrokes.Clear();
        }
    }
}
