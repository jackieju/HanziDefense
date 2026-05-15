using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HanziZombieDefense.Core
{
    /// <summary>
    /// High-level game state. Drives UI flow, input gating, and scene loading.
    /// </summary>
    public enum GameState
    {
        /// <summary>Boot/initial state before any user interaction.</summary>
        Menu,
        /// <summary>Active gameplay; input enabled, time scale = 1.</summary>
        Playing,
        /// <summary>Gameplay halted; time scale = 0.</summary>
        Paused,
        /// <summary>Player lost; awaits return-to-menu / retry.</summary>
        GameOver
    }

    /// <summary>
    /// Singleton entry point that owns the global <see cref="GameState"/>,
    /// publishes state transitions through <see cref="EventBus"/>, and
    /// orchestrates scene loading (Boot → MainMenu → Gameplay).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>Scene name constants matching Build Settings entries.</summary>
        public const string BootScene = "Boot";
        public const string MainMenuScene = "MainMenu";
        public const string GameplayScene = "Gameplay";

        private static GameManager _instance;

        /// <summary>Global access to the persistent <see cref="GameManager"/>.</summary>
        public static GameManager Instance
        {
            get => _instance;
        }

        [Header("Configuration")]
        [Tooltip("Auto-load MainMenu scene on Awake when starting from Boot.")]
        [SerializeField] private bool autoLoadMainMenuOnBoot = true;

        /// <summary>Current top-level state.</summary>
        public GameState CurrentState { get; private set; } = GameState.Menu;

        /// <summary>True while <see cref="CurrentState"/> is <see cref="GameState.Playing"/>.</summary>
        public bool IsPlaying => CurrentState == GameState.Playing;

        /// <summary>True while <see cref="CurrentState"/> is <see cref="GameState.Paused"/>.</summary>
        public bool IsPaused => CurrentState == GameState.Paused;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoLoadMainMenuOnBoot &&
                SceneManager.GetActiveScene().name == BootScene)
            {
                LoadScene(MainMenuScene);
            }
            else if (SceneManager.GetActiveScene().name == GameplayScene)
            {
                SetState(GameState.Playing);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Transition into <see cref="GameState.Playing"/>. Loads the Gameplay scene
        /// if it is not already active and resets <c>Time.timeScale</c>.
        /// </summary>
        public void StartGame()
        {
            Time.timeScale = 1f;

            if (SceneManager.GetActiveScene().name != GameplayScene)
            {
                // Subscribe once so the state flips to Playing only after load completes.
                SceneManager.sceneLoaded += HandleGameplayLoaded;
                LoadScene(GameplayScene);
            }
            else
            {
                SetState(GameState.Playing);
            }
        }

        private void HandleGameplayLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameplayScene) return;
            SceneManager.sceneLoaded -= HandleGameplayLoaded;
            SetState(GameState.Playing);
        }

        /// <summary>Pause gameplay. No-op unless currently <see cref="GameState.Playing"/>.</summary>
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        /// <summary>Resume gameplay. No-op unless currently <see cref="GameState.Paused"/>.</summary>
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        /// <summary>Transition to <see cref="GameState.GameOver"/>.</summary>
        public void GameOver()
        {
            if (CurrentState == GameState.GameOver) return;
            Time.timeScale = 0f;
            SetState(GameState.GameOver);
        }

        /// <summary>Return to the main menu, resetting time scale and clearing gameplay scene.</summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SetState(GameState.Menu);
            LoadScene(MainMenuScene);
        }

        private void SetState(GameState next)
        {
            if (next == CurrentState) return;

            var previous = CurrentState;
            CurrentState = next;

            EventBus.Publish(new GameEvents.GameStateChanged
            {
                previousState = previous,
                newState = next
            });
        }

        private static void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[GameManager] LoadScene called with empty name.");
                return;
            }
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}
