using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HanziZombieDefense.Hanzi.Input
{
    /// <summary>
    /// Full-screen, always-active drawing surface for the mobile build.
    /// The entire screen is the writing area; the player can draw a stroke
    /// anywhere on it. The host <see cref="Image"/> is configured as a
    /// raycast-target with a transparent (or low-alpha) tint so the gameplay
    /// scene remains visible behind the ink layer.
    ///
    /// Pointer events from the EventSystem (which automatically dispatches
    /// touch events on mobile) are translated into local-space points and
    /// forwarded to the connected <see cref="StrokeRecorder"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class WritingCanvas : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("Recorder that accumulates points into stroke lists.")]
        [SerializeField] private StrokeRecorder recorder;

        [Tooltip("Camera used by the parent Canvas. Leave null for ScreenSpace-Overlay canvases.")]
        [SerializeField] private Camera uiCamera;

        [Tooltip("Stretch the host RectTransform to fill the parent on Awake. Recommended for mobile full-screen.")]
        [SerializeField] private bool stretchToFullScreen = true;

        [Tooltip("Background tint applied at Awake. Alpha controls how much of the gameplay is visible through the canvas.")]
        [SerializeField] private Color backgroundTint = new Color(0f, 0f, 0f, 0.1f);

        /// <summary>Fired with the local-space start point when a stroke begins.</summary>
        public event Action<Vector2> StrokeStarted;

        /// <summary>Fired with each accepted point while a stroke is in progress.</summary>
        public event Action<Vector2> StrokeUpdated;

        /// <summary>Fired when a stroke ends; payload is the completed point list (read-only view).</summary>
        public event Action<List<Vector2>> StrokeEnded;

        private RectTransform _rect;
        private Image _image;
        private bool _drawing;

        /// <summary>True between <see cref="OnPointerDown"/> and <see cref="OnPointerUp"/>.</summary>
        public bool IsDrawing => _drawing;

        /// <summary>RectTransform of this canvas — useful for conversions in sibling systems.</summary>
        public RectTransform Rect => _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();

            _image.raycastTarget = true;
            _image.color = backgroundTint;

            if (stretchToFullScreen)
            {
                _rect.anchorMin = Vector2.zero;
                _rect.anchorMax = Vector2.one;
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
                _rect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>Drop all recorder state. Safe to call mid-stroke.</summary>
        public void Clear()
        {
            if (_drawing) AbortStroke();
            if (recorder != null) recorder.ClearAll();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!TryGetLocalPoint(eventData, out var local)) return;

            _drawing = true;
            recorder?.BeginStroke(local);
            StrokeStarted?.Invoke(local);
            StrokeUpdated?.Invoke(local);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_drawing) return;
            if (!TryGetLocalPoint(eventData, out var local)) return;

            if (recorder != null && recorder.AddPoint(local))
                StrokeUpdated?.Invoke(local);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_drawing) return;
            _drawing = false;

            if (TryGetLocalPoint(eventData, out var local))
                recorder?.AddPoint(local, force: true);

            var stroke = recorder?.EndStroke();
            StrokeEnded?.Invoke(stroke);
        }

        private void AbortStroke()
        {
            _drawing = false;
            recorder?.EndStroke();
        }

        private bool TryGetLocalPoint(PointerEventData ev, out Vector2 local)
        {
            local = default;
            var canvas = GetComponentInParent<Canvas>();
            Camera cam = null;

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = uiCamera != null ? uiCamera : canvas.worldCamera;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rect, ev.position, cam, out local);
        }
    }
}
