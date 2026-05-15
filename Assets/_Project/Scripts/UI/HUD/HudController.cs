using UnityEngine;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.UI.HUD
{
    /// <summary>
    /// Root HUD coordinator. Toggles its <see cref="GameObject"/> based on the
    /// current <see cref="GameState"/> so the HUD only appears during gameplay.
    /// Holds (optional) inspector refs to the individual HUD widgets — they
    /// can also live anywhere under this transform; the controller does not drive them directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private HealthBarUI healthBar;
        [SerializeField] private ScoreUI scoreUI;
        [SerializeField] private ComboUI comboUI;

        public HealthBarUI HealthBar => healthBar;
        public ScoreUI ScoreUI => scoreUI;
        public ComboUI ComboUI => comboUI;

        private void Awake()
        {
            if (hudRoot == null) hudRoot = gameObject;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.GameStateChanged>(OnGameStateChanged);

            var gm = GameManager.Instance;
            ApplyVisibility(gm != null ? gm.CurrentState : GameState.Menu);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.GameStateChanged>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameEvents.GameStateChanged evt)
        {
            ApplyVisibility(evt.newState);
        }

        private void ApplyVisibility(GameState state)
        {
            bool visible = state == GameState.Playing || state == GameState.Paused;
            if (hudRoot != null && hudRoot.activeSelf != visible)
            {
                hudRoot.SetActive(visible);
            }
        }
    }
}
