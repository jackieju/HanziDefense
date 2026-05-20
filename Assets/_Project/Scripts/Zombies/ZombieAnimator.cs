using UnityEngine;
using UnityEngine.AI;

namespace HanziZombieDefense.Zombies
{
    [RequireComponent(typeof(Animator))]
    public class ZombieAnimator : MonoBehaviour
    {
        [Header("Param Names")]
        [SerializeField] private string spawnTrigger = "Spawn";
        [SerializeField] private string walkBool = "IsWalking";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string deathTrigger = "Die";

        [SerializeField] private Zombie zombie;

        [Header("Speed Sync")]
        [SerializeField] private float animationBaseSpeed = 1.5f;

        private Animator _animator;
        private NavMeshAgent _agent;
        private int _spawnHash;
        private int _walkHash;
        private int _attackHash;
        private int _deathHash;
        private bool _hasController;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) _agent = GetComponentInParent<NavMeshAgent>();
            if (zombie == null) zombie = GetComponent<Zombie>();

            _hasController = _animator != null && _animator.runtimeAnimatorController != null;

            _spawnHash = Animator.StringToHash(spawnTrigger);
            _walkHash = Animator.StringToHash(walkBool);
            _attackHash = Animator.StringToHash(attackTrigger);
            _deathHash = Animator.StringToHash(deathTrigger);
        }

        private void OnEnable()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _hasController = _animator != null && _animator.runtimeAnimatorController != null;
            if (zombie != null)
            {
                zombie.StateChanged += HandleStateChanged;
                if (_hasController && zombie.State == ZombieState.Approaching)
                    PlayWalk();
            }
        }

        private void OnDisable()
        {
            if (zombie != null) zombie.StateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (!_hasController || _animator == null || _agent == null) return;
            float speed = _agent.velocity.magnitude;
            _animator.speed = speed > 0.1f ? speed / animationBaseSpeed : 1f;
        }

        private void HandleStateChanged(ZombieState previous, ZombieState current)
        {
            if (!_hasController) return;

            switch (current)
            {
                case ZombieState.Spawning: PlaySpawn(); break;
                case ZombieState.Approaching: PlayWalk(); break;
                case ZombieState.Attacking: PlayAttack(); break;
                case ZombieState.Dying: PlayDeath(); break;
                case ZombieState.Dead: _animator.SetBool(_walkHash, false); break;
            }
        }

        /// <summary>Fire the spawn-out-of-ground animation trigger.</summary>
        public void PlaySpawn()
        {
            _animator.ResetTrigger(_attackHash);
            _animator.ResetTrigger(_deathHash);
            _animator.SetBool(_walkHash, false);
            _animator.SetTrigger(_spawnHash);
        }

        /// <summary>Enter the walking loop.</summary>
        public void PlayWalk()
        {
            _animator.SetBool(_walkHash, true);
        }

        /// <summary>Trigger the attack animation. Walking stays true so the agent can resume.</summary>
        public void PlayAttack()
        {
            _animator.SetBool(_walkHash, false);
            _animator.SetTrigger(_attackHash);
        }

        /// <summary>Play the death animation and stop walking.</summary>
        public void PlayDeath()
        {
            _animator.SetBool(_walkHash, false);
            _animator.SetTrigger(_deathHash);
        }
    }
}
