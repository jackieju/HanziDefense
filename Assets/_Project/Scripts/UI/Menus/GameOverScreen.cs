using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HanziZombieDefense.Core;
using HanziZombieDefense.Scoring;

namespace HanziZombieDefense.UI.Menus
{
    /// <summary>
    /// Game-over panel. Subscribes to <see cref="GameEvents.PlayerDied"/>, displays
    /// final score / high score / kill count, and exposes Retry / Main Menu buttons.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [SerializeField] private ScoreManager scoreManager;

        private void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);

            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.PlayerDied>(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PlayerDied>(OnPlayerDied);
        }

        private void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenu);
        }

        private void OnPlayerDied(GameEvents.PlayerDied _)
        {
            if (scoreManager == null) ServiceLocator.TryGet(out scoreManager);

            if (panel != null) panel.SetActive(true);

            if (scoreManager != null)
            {
                if (finalScoreText != null) finalScoreText.text = $"Score: {scoreManager.Score:N0}";
                if (highScoreText != null) highScoreText.text = $"Best:  {scoreManager.HighScore:N0}";
                if (killsText != null) killsText.text = $"Kills: {scoreManager.KillsThisRun}";
            }
            else
            {
                if (finalScoreText != null) finalScoreText.text = "Score: -";
                if (highScoreText != null) highScoreText.text = "Best:  -";
                if (killsText != null) killsText.text = "Kills: -";
            }

            var gm = GameManager.Instance;
            if (gm != null) gm.GameOver();
        }

        private void OnRetry()
        {
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;

            if (scoreManager != null) scoreManager.ResetRun();

            var gm = GameManager.Instance;
            if (gm != null) gm.StartGame();
        }

        private void OnMainMenu()
        {
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;

            var gm = GameManager.Instance;
            if (gm != null) gm.ReturnToMainMenu();
        }
    }
}
