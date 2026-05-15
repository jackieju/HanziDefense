using System.Collections;
using TMPro;
using UnityEngine;
using HanziZombieDefense.Core;
using HanziZombieDefense.Scoring;

namespace HanziZombieDefense.UI.HUD
{
    /// <summary>
    /// Displays the current score in a <see cref="TextMeshProUGUI"/> and plays a
    /// brief punch-scale animation whenever the score increases.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScoreUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI scoreText;

        [SerializeField, Tooltip("Transform to scale-punch on score change. Defaults to scoreText's transform.")]
        private RectTransform punchTarget;

        [SerializeField, Min(1f)] private float punchScale = 1.25f;
        [SerializeField, Min(0.05f)] private float punchDuration = 0.18f;

        [SerializeField, Tooltip("Optional explicit reference to the ScoreManager. Resolved via ServiceLocator if null.")]
        private ScoreManager scoreManager;

        private Vector3 _restScale = Vector3.one;
        private Coroutine _punchRoutine;
        private int _displayedScore;

        private void Awake()
        {
            if (punchTarget == null && scoreText != null)
            {
                punchTarget = scoreText.rectTransform;
            }
            if (punchTarget != null) _restScale = punchTarget.localScale;
        }

        private void OnEnable()
        {
            if (scoreManager == null)
            {
                ServiceLocator.TryGet(out scoreManager);
            }

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged += OnScoreChanged;
                _displayedScore = scoreManager.Score;
            }

            UpdateText(_displayedScore);
        }

        private void OnDisable()
        {
            if (scoreManager != null) scoreManager.ScoreChanged -= OnScoreChanged;
        }

        private void OnScoreChanged(int newScore)
        {
            bool increased = newScore > _displayedScore;
            _displayedScore = newScore;
            UpdateText(newScore);

            if (increased) PlayPunch();
        }

        private void UpdateText(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString("N0");
        }

        private void PlayPunch()
        {
            if (punchTarget == null) return;
            if (_punchRoutine != null) StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            float t = 0f;
            float half = punchDuration * 0.5f;
            Vector3 peak = _restScale * punchScale;

            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                punchTarget.localScale = Vector3.Lerp(_restScale, peak, t / half);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                punchTarget.localScale = Vector3.Lerp(peak, _restScale, t / half);
                yield return null;
            }

            punchTarget.localScale = _restScale;
            _punchRoutine = null;
        }
    }
}
