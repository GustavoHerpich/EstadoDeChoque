using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using UnityEngine;
using UnityEngine.Events;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore
{
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Player/Movement")]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private FirstPersonPlayerSettings _settings;

        [SerializeField]
        private FirstPersonInputSource _inputSource;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<float> _onStaminaChanged;

        [SerializeField]
        private UnityEvent<Vector3> _onVelocityChanged;

        [SerializeField]
        private UnityEvent<bool> _onGroundedChanged;

        [SerializeField]
        private UnityEvent _onJump;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _verticalVelocity;
        private float _stamina;
        private float _staminaCooldown;
        private bool _enabled = true;

        public Vector3 Velocity => _velocity;
        public float VerticalVelocity => _verticalVelocity;
        public float Stamina => _stamina;
        public bool IsGrounded => _controller?.isGrounded ?? false;
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_settings == null)
            {
                _settings = FirstPersonPlayerSettings.CreateRuntimeDefaults();
            }
            _stamina = _settings.MaxStamina;
        }

        private void Update()
        {
            if (_enabled && _inputSource != null)
            {
                PlayerInputFrame input = _inputSource.ReadFrame();
                ApplyMovement(input);
                UpdateStamina(input);
            }
        }

        private void ApplyMovement(PlayerInputFrame input)
        {
            var crouched = input.CrouchHeld && CanStandUp() == false;
            UpdateHeight(crouched);

            Vector3 direction = (
                transform.right * input.Move.x + transform.forward * input.Move.y
            ).normalized;
            var sprinting =
                input.SprintHeld
                && input.Move.sqrMagnitude > 0.01f
                && _stamina > 0.05f
                && !crouched;
            var speed = crouched
                ? _settings.CrouchSpeed
                : (sprinting ? _settings.SprintSpeed : _settings.WalkSpeed);

            _velocity = Vector3.MoveTowards(
                _velocity,
                direction * speed,
                _settings.Acceleration * Time.deltaTime
            );

            if (_controller.isGrounded)
            {
                _verticalVelocity = _settings.GroundedSnapVelocity;
                if (input.JumpPressed && !crouched)
                {
                    _verticalVelocity = Mathf.Sqrt(_settings.JumpHeight * -2f * _settings.Gravity);
                    _onJump?.Invoke();
                }
            }
            else
            {
                _verticalVelocity += _settings.Gravity * Time.deltaTime;
            }

            _controller.Move(
                new Vector3(_velocity.x, _verticalVelocity, _velocity.z) * Time.deltaTime
            );

            if (_controller.isGrounded && _verticalVelocity < _settings.GroundedSnapVelocity)
            {
                _verticalVelocity = _settings.GroundedSnapVelocity;
            }

            _onVelocityChanged?.Invoke(_velocity);
            _onGroundedChanged?.Invoke(_controller.isGrounded);
        }

        private void UpdateStamina(PlayerInputFrame input)
        {
            var isMoving = _velocity.sqrMagnitude > 0.01f;
            var isRunning = input.SprintHeld && isMoving;

            if (isRunning)
            {
                _stamina = Mathf.Max(
                    0f,
                    _stamina - _settings.SprintDrainPerSecond * Time.deltaTime
                );
                _staminaCooldown = _settings.StaminaRegenDelay;
            }
            else
            {
                _staminaCooldown = Mathf.Max(0f, _staminaCooldown - Time.deltaTime);
                if (_staminaCooldown <= 0f)
                {
                    _stamina = Mathf.Min(
                        _settings.MaxStamina,
                        _stamina + _settings.StaminaRegenPerSecond * Time.deltaTime
                    );
                }
            }

            _onStaminaChanged?.Invoke(_stamina);
        }

        private void UpdateHeight(bool crouched)
        {
            var target = crouched ? _settings.CrouchingHeight : _settings.StandingHeight;
            _controller.height = Mathf.Lerp(
                _controller.height,
                target,
                1f - Mathf.Exp(-_settings.HeightLerpSpeed * Time.deltaTime)
            );
            _controller.center = new Vector3(0f, _controller.height * 0.5f, 0f);
        }

        private bool CanStandUp()
        {
            var r = Mathf.Max(0.01f, _controller.radius * 0.95f);
            return !Physics.CheckCapsule(
                transform.position + Vector3.up * r,
                transform.position + Vector3.up * (_settings.StandingHeight - r),
                r,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        public void SetSettings(FirstPersonPlayerSettings settings)
        {
            _settings = settings;
        }

        public void SetInput(FirstPersonInputSource input)
        {
            _inputSource = input;
        }
    }
}
