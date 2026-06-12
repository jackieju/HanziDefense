using System.Collections;
using UnityEngine;
using HanziZombieDefense.Audio;
using HanziZombieDefense.Core;
using HanziZombieDefense.VFX;

namespace HanziZombieDefense.Zombies
{
    public class ZombieDeathFX : MonoBehaviour
    {
        [SerializeField] private Zombie zombie;

        [SerializeField]
        private float fxDuration = 1.25f;

        [SerializeField]
        private float verticalOffset = 1.0f;

        [SerializeField]
        private string deathSoundId = "zombie_death";

        private Coroutine _routine;

        private void Awake()
        {
            if (zombie == null) zombie = GetComponent<Zombie>();
        }

        private void OnEnable()
        {
            if (zombie != null) zombie.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (zombie != null) zombie.StateChanged -= HandleStateChanged;
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private void HandleStateChanged(ZombieState previous, ZombieState current)
        {
            if (current != ZombieState.Dying) return;
            if (_routine != null) return;
            _routine = StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            Vector3 spawnPos = transform.position + Vector3.up * verticalOffset;

            if (zombie != null && zombie.Label != null)
                zombie.Label.gameObject.SetActive(false);

            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = false;

            if (ServiceLocator.TryGet<ExplosionPool>(out var explosions))
            {
                explosions.SpawnExplosion(spawnPos);
            }

            if (!string.IsNullOrEmpty(deathSoundId))
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(deathSoundId);
                }
            }

            yield return new WaitForSeconds(fxDuration);

            if (zombie != null)
            {
                zombie.TransitionTo(ZombieState.Dead);
                zombie.gameObject.SetActive(false);
            }

            _routine = null;
        }
    }
}
