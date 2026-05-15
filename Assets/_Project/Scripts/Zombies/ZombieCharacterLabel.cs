using TMPro;
using UnityEngine;

namespace HanziZombieDefense.Zombies
{
    /// <summary>
    /// World-space label that displays the zombie's assigned hanzi character above its
    /// head. Billboards toward the active <see cref="Camera"/> every <c>LateUpdate</c>
    /// and exposes a highlighted state for use when the zombie is the player's target.
    /// </summary>
    public class ZombieCharacterLabel : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_Text text;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightedColor = new Color(1f, 0.85f, 0.2f);

        [Header("Highlight Scale")]
        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float highlightedScale = 1.25f;
        [SerializeField] private float scaleLerpSpeed = 12f;

        private Transform _cameraTf;
        private bool _highlighted;
        private float _currentScale = 1f;

        private void Awake()
        {
            if (canvas == null) canvas = GetComponentInChildren<Canvas>(true);
            if (text == null && canvas != null) text = canvas.GetComponentInChildren<TMP_Text>(true);
            if (canvas != null) canvas.renderMode = RenderMode.WorldSpace;
            _currentScale = normalScale;
            ApplyVisuals(instant: true);
        }

        private void OnEnable()
        {
            ResolveCamera();
        }

        private void LateUpdate()
        {
            if (canvas == null) return;
            if (_cameraTf == null) ResolveCamera();
            if (_cameraTf == null) return;

            Transform t = canvas.transform;
            Vector3 toCam = t.position - _cameraTf.position;
            if (toCam.sqrMagnitude > 0.0001f)
            {
                t.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }

            float target = _highlighted ? highlightedScale : normalScale;
            _currentScale = Mathf.Lerp(_currentScale, target, Time.deltaTime * scaleLerpSpeed);
            t.localScale = new Vector3(_currentScale, _currentScale, _currentScale);
        }

        /// <summary>Set the displayed hanzi (or any short string).</summary>
        public void SetCharacter(string character)
        {
            if (text != null) text.text = character ?? string.Empty;
        }

        /// <summary>
        /// Toggle highlighted appearance (color + scale) used when this zombie is the
        /// player's current target.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted) return;
            _highlighted = highlighted;
            ApplyVisuals(instant: false);
        }

        private void ApplyVisuals(bool instant)
        {
            if (text != null) text.color = _highlighted ? highlightedColor : normalColor;
            if (instant)
            {
                _currentScale = _highlighted ? highlightedScale : normalScale;
                if (canvas != null) canvas.transform.localScale = Vector3.one * _currentScale;
            }
        }

        private void ResolveCamera()
        {
            Camera cam = Camera.main;
            if (cam != null) _cameraTf = cam.transform;
        }
    }
}
