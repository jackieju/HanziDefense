using UnityEngine;
using UnityEngine.UI;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.UI.HUD
{
    [RequireComponent(typeof(Image))]
    public class DamageVignette : MonoBehaviour
    {
        [SerializeField] private float flashAlpha = 0.6f;
        [SerializeField] private float lowHpAlpha = 0.3f;
        [SerializeField] private float lowHpThreshold = 0.3f;
        [SerializeField] private float flashFadeSpeed = 3f;
        [SerializeField] private float pulseSpeed = 2f;

        private Image _image;
        private float _flashTimer;
        private float _currentHpNormalized = 1f;
        private float _maxHealth = 100f;

        private void Awake()
        {
            _image = GetComponent<Image>();
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.PlayerDamaged>(OnDamaged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PlayerDamaged>(OnDamaged);
        }

        private void OnDamaged(GameEvents.PlayerDamaged evt)
        {
            _flashTimer = 1f;
            _maxHealth = Mathf.Max(1f, evt.MaxHealth);
            _currentHpNormalized = Mathf.Clamp01(evt.CurrentHealth / _maxHealth);
        }

        private void Update()
        {
            float alpha = 0f;

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime * flashFadeSpeed;
                alpha = Mathf.Max(alpha, flashAlpha * Mathf.Clamp01(_flashTimer));
            }

            if (_currentHpNormalized <= lowHpThreshold && _currentHpNormalized > 0f)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                float lowHpIntensity = 1f - (_currentHpNormalized / lowHpThreshold);
                alpha = Mathf.Max(alpha, lowHpAlpha * lowHpIntensity * (0.5f + 0.5f * pulse));
            }

            SetAlpha(alpha);
        }

        private void SetAlpha(float a)
        {
            if (_image == null) return;
            var c = _image.color;
            c.a = a;
            _image.color = c;
        }
    }
}
