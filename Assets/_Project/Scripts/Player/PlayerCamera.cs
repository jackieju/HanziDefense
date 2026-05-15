using System.Collections;
using UnityEngine;
using HanziZombieDefense.Core;

namespace HanziZombieDefense.Player
{
    /// <summary>
    /// Static camera with damage-shake. The mobile build keeps the camera
    /// fixed in place — no walking head-bob, no mouse look. Auto-shakes when
    /// <see cref="GameEvents.PlayerDamaged"/> is published.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Hit Shake")]
        [SerializeField, Tooltip("Multiplier applied to the intensity passed to ShakeCamera when reacting to PlayerDamaged events.")]
        private float damageShakeIntensity = 0.25f;

        [SerializeField] private float damageShakeDuration = 0.2f;

        private Vector3 _restLocalPos;
        private Vector3 _shakeOffset;
        private Coroutine _shakeRoutine;

        private void Awake()
        {
            _restLocalPos = transform.localPosition;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.PlayerDamaged>(OnPlayerDamaged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.PlayerDamaged>(OnPlayerDamaged);
            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                _shakeRoutine = null;
            }
            _shakeOffset = Vector3.zero;
            transform.localPosition = _restLocalPos;
        }

        private void LateUpdate()
        {
            transform.localPosition = _restLocalPos + _shakeOffset;
        }

        /// <summary>
        /// Shake the camera with the given peak <paramref name="intensity"/> (in local units)
        /// for <paramref name="duration"/> seconds. Restarts any in-flight shake.
        /// </summary>
        public void ShakeCamera(float intensity, float duration)
        {
            if (intensity <= 0f || duration <= 0f) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float falloff = 1f - (elapsed / duration);
                _shakeOffset = Random.insideUnitSphere * intensity * falloff;
                elapsed += Time.deltaTime;
                yield return null;
            }
            _shakeOffset = Vector3.zero;
            _shakeRoutine = null;
        }

        private void OnPlayerDamaged(GameEvents.PlayerDamaged evt)
        {
            float scaled = damageShakeIntensity * Mathf.Clamp01(evt.Amount / Mathf.Max(1f, evt.MaxHealth) * 4f);
            ShakeCamera(Mathf.Max(damageShakeIntensity * 0.5f, scaled), damageShakeDuration);
        }
    }
}
