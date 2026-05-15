using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HanziZombieDefense.Hanzi.Input
{
    /// <summary>
    /// Renders strokes on a UI canvas as collections of thin <see cref="Image"/>
    /// rectangles connecting consecutive points. Each stroke owns a parent
    /// GameObject so it can be recolored or destroyed atomically.
    ///
    /// Trade-offs vs. a mesh / RenderTexture approach: simpler, no extra
    /// dependencies, draw cost is O(points) GameObjects per stroke. Suitable
    /// for short hanzi strokes (≤ ~200 segments per stroke).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class StrokeRenderer : MonoBehaviour
    {
        [Tooltip("Line thickness in canvas pixels.")]
        [SerializeField] private float lineWidth = 6f;

        [Tooltip("Default color for strokes still pending recognition.")]
        [SerializeField] private Color pendingColor = Color.white;

        [Tooltip("Color applied to a stroke that the matcher accepted.")]
        [SerializeField] private Color confirmedColor = new Color(0.2f, 1f, 0.4f, 1f);

        [Tooltip("Color applied to a rejected stroke (briefly, before it is removed).")]
        [SerializeField] private Color rejectedColor = new Color(1f, 0.3f, 0.3f, 1f);

        [Tooltip("Optional sprite for line segments. If null, segments use the default UI sprite.")]
        [SerializeField] private Sprite segmentSprite;

        private readonly List<RectTransform> _strokeRoots = new List<RectTransform>();
        private RectTransform _hostRect;

        /// <summary>Number of strokes currently rendered.</summary>
        public int StrokeCount => _strokeRoots.Count;

        private void Awake()
        {
            _hostRect = GetComponent<RectTransform>();
        }

        // ─────────────────────────── Public API ───────────────────────────

        /// <summary>Render <paramref name="points"/> as a new polyline. Returns the index of the added stroke.</summary>
        public int DrawStroke(List<Vector2> points, Color color)
        {
            var root = CreateStrokeRoot();
            _strokeRoots.Add(root);

            if (points == null || points.Count < 2) return _strokeRoots.Count - 1;

            for (int i = 1; i < points.Count; i++)
                CreateLineSegment(root, points[i - 1], points[i], lineWidth, color);

            return _strokeRoots.Count - 1;
        }

        /// <summary>Render with the configured pending color.</summary>
        public int DrawStrokePending(List<Vector2> points) => DrawStroke(points, pendingColor);

        /// <summary>Recolor a previously drawn stroke as confirmed (green).</summary>
        public void ConfirmStroke(int index) => RecolorStroke(index, confirmedColor);

        /// <summary>Recolor a previously drawn stroke as rejected (red).</summary>
        public void RejectStroke(int index) => RecolorStroke(index, rejectedColor);

        /// <summary>Destroy the most recently added stroke. No-op if empty.</summary>
        public void RemoveLastStroke()
        {
            if (_strokeRoots.Count == 0) return;
            int last = _strokeRoots.Count - 1;
            var root = _strokeRoots[last];
            _strokeRoots.RemoveAt(last);
            if (root != null) Destroy(root.gameObject);
        }

        /// <summary>Destroy every rendered stroke.</summary>
        public void ClearAll()
        {
            for (int i = 0; i < _strokeRoots.Count; i++)
                if (_strokeRoots[i] != null) Destroy(_strokeRoots[i].gameObject);
            _strokeRoots.Clear();
        }

        // ─────────────────────────── Construction ───────────────────────────

        private RectTransform CreateStrokeRoot()
        {
            var go = new GameObject($"Stroke_{_strokeRoots.Count}", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_hostRect, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        /// <summary>
        /// Build one straight segment as a thin axis-aligned rectangle, then
        /// rotate it to span <paramref name="from"/> → <paramref name="to"/>.
        /// </summary>
        private void CreateLineSegment(RectTransform parent, Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 dir = to - from;
            float length = dir.magnitude;
            if (length <= float.Epsilon) return;

            var go = new GameObject("Segment", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(length, width);
            rt.anchoredPosition = (from + to) * 0.5f;

            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

            var img = go.GetComponent<Image>();
            if (segmentSprite != null) img.sprite = segmentSprite;
            img.color = color;
            img.raycastTarget = false;
        }

        private void RecolorStroke(int index, Color color)
        {
            if (index < 0 || index >= _strokeRoots.Count) return;
            var root = _strokeRoots[index];
            if (root == null) return;

            var images = root.GetComponentsInChildren<Image>(includeInactive: true);
            for (int i = 0; i < images.Length; i++) images[i].color = color;
        }
    }
}
