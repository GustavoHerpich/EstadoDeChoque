using UnityEngine;
using UnityEngine.Events;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore
{
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Player/Animation")]
    public sealed class PlayerAnimation : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private PlayerMovement _movement;

        [Header("Events")]
        [SerializeField]
        private UnityEvent _onLand;

        private Animator _animator;
        private int _speedHash;
        private int _groundedHash;
        private bool _wasGrounded;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _speedHash = Animator.StringToHash("Speed");
            _groundedHash = Animator.StringToHash("Grounded");
        }

        private void Update()
        {
            if (_animator == null || _movement == null)
            {
                return;
            }

            var speed = _movement.Velocity.magnitude;
            _animator.SetFloat(_speedHash, speed, 0.1f, Time.deltaTime);

            var isGrounded = _movement.IsGrounded;
            _animator.SetBool(_groundedHash, isGrounded);

            if (!_wasGrounded && isGrounded)
            {
                _onLand?.Invoke();
            }

            _wasGrounded = isGrounded;
        }

        public void SetMovement(PlayerMovement movement)
        {
            _movement = movement;
        }
    }
}
