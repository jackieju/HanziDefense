using TMPro;
using UnityEngine;
using HanziZombieDefense.Scoring;

namespace HanziZombieDefense.UI.HUD
{
    /// <summary>
    /// Shows current combo + multiplier. Fades the <see cref="CanvasGroup"/> in
    /// when combo > 1, fades out when combo resets.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ComboUI : MonoBehaviour
    {
        [SerializeField] private ComboTracker comboTracker;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private TextMeshProUGUI multiplierText;

        [SerializeField, Min(0.1f)] private float fadeSpeed = 6f;

        private CanvasGroup _group;
        private float _targetAlpha;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _targetAlpha = 0f;
        }

        private void OnEnable()
        {
            if (comboTracker == null) comboTracker = FindObjectOfType<ComboTracker>();
            if (comboTracker != null)
            {
                comboTracker.ComboChanged += OnComboChanged;
                Apply(comboTracker.CurrentCombo);
            }
        }

        private void OnDisable()
        {
            if (comboTracker != null) comboTracker.ComboChanged -= OnComboChanged;
        }

        private void Update()
        {
            if (_group == null) return;
            if (!Mathf.Approximately(_group.alpha, _targetAlpha))
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
            }
        }

        private void OnComboChanged(int combo) => Apply(combo);

        private void Apply(int combo)
        {
            _targetAlpha = combo > 1 ? 1f : 0f;

            if (comboText != null)
            {
                comboText.text = combo > 0 ? $"x{combo}" : string.Empty;
            }
            if (multiplierText != null && comboTracker != null)
            {
                multiplierText.text = $"{comboTracker.ComboMultiplier:0.0}x";
            }
        }
    }
}
