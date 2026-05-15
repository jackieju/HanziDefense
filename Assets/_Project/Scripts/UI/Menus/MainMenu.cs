using UnityEngine;
using UnityEngine.UI;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.UI.Menus
{
    /// <summary>Main menu controller. Wires three buttons (Start / Options / Quit) to game flow.</summary>
    [DisallowMultipleComponent]
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject optionsPanel;

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (optionsButton != null) optionsButton.onClick.AddListener(OnOptions);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            if (optionsPanel != null) optionsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStart);
            if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptions);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        private void OnStart()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.StartGame();
            else Debug.LogError("[MainMenu] GameManager.Instance is null.");
        }

        private void OnOptions()
        {
            if (optionsPanel != null) optionsPanel.SetActive(!optionsPanel.activeSelf);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
