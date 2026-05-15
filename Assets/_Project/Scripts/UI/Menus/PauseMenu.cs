using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.UI.Menus
{
    /// <summary>
    /// Pause menu toggled with the Escape key. Drives <see cref="GameManager.PauseGame"/> /
    /// <see cref="GameManager.ResumeGame"/> and exposes Resume / MainMenu / Quit buttons.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        private bool _isOpen;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            ShowPanel(false);
        }

        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResume);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenu);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        /// <summary>Flip pause state. Pauses if currently <see cref="GameState.Playing"/>, resumes if <see cref="GameState.Paused"/>.</summary>
        public void Toggle()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.CurrentState == GameState.Playing)
            {
                gm.PauseGame();
                Time.timeScale = 0f;
                ShowPanel(true);
            }
            else if (gm.CurrentState == GameState.Paused)
            {
                gm.ResumeGame();
                Time.timeScale = 1f;
                ShowPanel(false);
            }
        }

        private void OnResume()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.ResumeGame();
            Time.timeScale = 1f;
            ShowPanel(false);
        }

        private void OnMainMenu()
        {
            Time.timeScale = 1f;
            ShowPanel(false);
            var gm = GameManager.Instance;
            if (gm != null) gm.ReturnToMainMenu();
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowPanel(bool show)
        {
            _isOpen = show;
            if (pausePanel != null) pausePanel.SetActive(show);
        }
    }
}
