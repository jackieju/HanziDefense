using UnityEngine;
using HanziZombieDefense.Player;

namespace HanziZombieDefense.Zombies
{
    public class ZombieAttack : MonoBehaviour
    {
        [SerializeField] private Zombie zombie;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float attackInterval = 1.5f;

        private PlayerHealth _playerHealth;
        private float _lastAttackTime;

        private void Awake()
        {
            if (zombie == null) zombie = GetComponent<Zombie>();
        }

        private void OnEnable()
        {
            if (zombie != null) zombie.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (zombie != null) zombie.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(ZombieState previous, ZombieState current)
        {
            if (current == ZombieState.Attacking)
            {
                Attack();
            }
        }

        private void Update()
        {
            if (zombie == null || zombie.State != ZombieState.Attacking) return;
            if (Time.time - _lastAttackTime >= attackInterval)
            {
                Attack();
            }
        }

        private void Attack()
        {
            if (_playerHealth == null)
                _playerHealth = FindObjectOfType<PlayerHealth>();

            if (_playerHealth == null || _playerHealth.IsDead) return;

            _playerHealth.TakeDamage(damage);
            _lastAttackTime = Time.time;
        }
    }
}
