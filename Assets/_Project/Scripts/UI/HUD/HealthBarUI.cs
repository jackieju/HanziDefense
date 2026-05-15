using UnityEngine;
using UnityEngine.UI;
using HanziZombieDefense.Core;
using HanziZombieDefense.Player;

namespace HanziZombieDefense.UI.HUD
{
    /// <summary>
    /// Drives a filled <see cref="Image"/> (Filled type) from the player's health.
    /// Reacts to <see cref="GameEvents.PlayerDamaged"/> and lerps smoothly toward the target fill.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image healthBar;

        [SerializeField, Tooltip("Optional. If set, the bar reads CurrentHealth/MaxHealth on Enable to initialize correctly.")]
        private PlayerHealth playerHealth;

        [SerializeField, Min(0.1f), Tooltip("Lerp speed (1/seconds) for the bar fill.")]
        private float lerpSpeed = 8f;

        [SerializeField, Tooltip("Color blend at full health.")]
        private Color fullColor = new Color(0.2f, 0.85f, 0.3f);

        [SerializeField, Tooltip("Color blend at empty health.")]
        private Color emptyColor = new Color(0.85f, 0.2f, 0.2f);

        private float _targetFill = 1f;
        private float _maxHpCached = 100f;

        private void Awake()
        {
            if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.PlayerDamaged>(OnPlayerDamaged);
            EventBus.Subscribe<GameEvents.PlayerDied>(OnPlayerDied);

            if (playerHealth != null)
            {
                _maxHpCached = Mathf.Max(1f, playerHealth.MaxHealth);
                _targetFill = Mathf.Clamp01(playerHealth.Normalized);
                ApplyFillImmediate(_targetFill);
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PlayerDamaged>(OnPlayerDamaged);
            EventBus.Unsubscribe<GameEvents.PlayerDied>(OnPlayerDied);
        }

        private void Update()
        {
            if (healthBar == null) return;

            if (!Mathf.Approximately(healthBar.fillAmount, _targetFill))
            {
                healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, _targetFill, Time.unscaledDeltaTime * lerpSpeed);
                if (Mathf.Abs(healthBar.fillAmount - _targetFill) < 0.001f)
                {
                    healthBar.fillAmount = _targetFill;
                }
                healthBar.color = Color.Lerp(emptyColor, fullColor, healthBar.fillAmount);
            }
        }

        private void OnPlayerDamaged(GameEvents.PlayerDamaged evt)
        {
            if (_maxHpCached <= 0f && playerHealth != null) _maxHpCached = Mathf.Max(1f, playerHealth.MaxHealth);

            float remaining = Mathf.Max(0f, evt.remainingHP);
            _targetFill = Mathf.Clamp01(remaining / Mathf.Max(1f, _maxHpCached));
        }

        private void OnPlayerDied(GameEvents.PlayerDied _)
        {
            _targetFill = 0f;
        }

        private void ApplyFillImmediate(float fill)
        {
            if (healthBar == null) return;
            healthBar.fillAmount = fill;
            healthBar.color = Color.Lerp(emptyColor, fullColor, fill);
        }
    }
}
