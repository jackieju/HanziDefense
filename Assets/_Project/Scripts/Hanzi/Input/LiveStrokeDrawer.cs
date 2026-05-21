using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.Hanzi.Input
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class LiveStrokeDrawer : MonoBehaviour
    {
        [SerializeField] private WritingCanvas writingCanvas;
        [SerializeField] private WritingSession writingSession;
        [SerializeField] private float lineWidth = 8f;
        [SerializeField] private Color drawingColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color matchedColor = new Color(0.2f, 1f, 0.4f, 1f);
        [SerializeField] private Color rejectedColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Sprite segmentSprite;

        private RectTransform _rect;
        private readonly List<GameObject> _confirmedStrokes = new List<GameObject>();
        private GameObject _currentStrokeParent;
        private Vector2 _lastPoint;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (writingCanvas == null) writingCanvas = GetComponent<WritingCanvas>();
            if (writingSession == null) writingSession = GetComponent<WritingSession>();
        }

        private void OnEnable()
        {
            if (writingCanvas != null)
            {
                writingCanvas.StrokeStarted += OnStrokeStarted;
                writingCanvas.StrokeUpdated += OnStrokeUpdated;
                writingCanvas.StrokeEnded += OnStrokeEnded;
            }

            EventBus.Subscribe<WritingEvents.StrokeAccepted>(OnStrokeAccepted);
            EventBus.Subscribe<WritingEvents.StrokeRejected>(OnStrokeRejected);
            EventBus.Subscribe<WritingEvents.WritingSessionStarted>(OnSessionStarted);
            EventBus.Subscribe<WritingEvents.WritingSessionCancelled>(OnSessionCancelled);
            EventBus.Subscribe<GameEvents.CharacterCompleted>(OnCharacterCompleted);
        }

        private void OnDisable()
        {
            if (writingCanvas != null)
            {
                writingCanvas.StrokeStarted -= OnStrokeStarted;
                writingCanvas.StrokeUpdated -= OnStrokeUpdated;
                writingCanvas.StrokeEnded -= OnStrokeEnded;
            }

            EventBus.Unsubscribe<WritingEvents.StrokeAccepted>(OnStrokeAccepted);
            EventBus.Unsubscribe<WritingEvents.StrokeRejected>(OnStrokeRejected);
            EventBus.Unsubscribe<WritingEvents.WritingSessionStarted>(OnSessionStarted);
            EventBus.Unsubscribe<WritingEvents.WritingSessionCancelled>(OnSessionCancelled);
            EventBus.Unsubscribe<GameEvents.CharacterCompleted>(OnCharacterCompleted);
        }

        private void OnStrokeStarted(Vector2 point)
        {
            if (writingSession != null && !writingSession.IsActive)
            {
                _currentStrokeParent = null;
                return;
            }

            _currentStrokeParent = new GameObject("LiveStroke");
            var rt = _currentStrokeParent.AddComponent<RectTransform>();
            rt.SetParent(_rect, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            _lastPoint = point;
        }

        private void OnStrokeUpdated(Vector2 point)
        {
            if (_currentStrokeParent == null) return;
            if (Vector2.Distance(point, _lastPoint) < 1f) return;

            CreateSegment(_currentStrokeParent.transform, _lastPoint, point, drawingColor);
            _lastPoint = point;
        }

        private void OnStrokeEnded(List<Vector2> points)
        {
            // Don't clear — wait for match/reject event to decide
        }

        private void OnStrokeAccepted(WritingEvents.StrokeAccepted evt)
        {
            if (_currentStrokeParent == null) return;

            var images = _currentStrokeParent.GetComponentsInChildren<Image>();
            for (int i = 0; i < images.Length; i++)
                images[i].color = matchedColor;

            _confirmedStrokes.Add(_currentStrokeParent);
            _currentStrokeParent = null;
        }

        private void OnStrokeRejected(WritingEvents.StrokeRejected evt)
        {
            if (_currentStrokeParent == null) return;

            var images = _currentStrokeParent.GetComponentsInChildren<Image>();
            for (int i = 0; i < images.Length; i++)
                images[i].color = rejectedColor;

            var toDestroy = _currentStrokeParent;
            _currentStrokeParent = null;
            Destroy(toDestroy, 0.3f);
        }

        private void OnSessionStarted(WritingEvents.WritingSessionStarted evt)
        {
            ClearAll();
        }

        private void OnSessionCancelled(WritingEvents.WritingSessionCancelled evt)
        {
            ClearAll();
        }

        private void OnCharacterCompleted(GameEvents.CharacterCompleted evt)
        {
            ClearAll();
        }

        private void CreateSegment(Transform parent, Vector2 from, Vector2 to, Color color)
        {
            Vector2 dir = to - from;
            float length = dir.magnitude;
            if (length <= 0.01f) return;

            var go = new GameObject("Seg", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(length, lineWidth);
            rt.anchoredPosition = (from + to) * 0.5f;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);

            var img = go.GetComponent<Image>();
            if (segmentSprite != null) img.sprite = segmentSprite;
            img.color = color;
            img.raycastTarget = false;
        }

        private void ClearAll()
        {
            if (_currentStrokeParent != null)
            {
                Destroy(_currentStrokeParent);
                _currentStrokeParent = null;
            }

            for (int i = 0; i < _confirmedStrokes.Count; i++)
            {
                if (_confirmedStrokes[i] != null)
                    Destroy(_confirmedStrokes[i]);
            }
            _confirmedStrokes.Clear();
        }
    }
}
