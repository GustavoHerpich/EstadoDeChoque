using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(FirstPersonInputSource))]
    [RequireComponent(typeof(PlayerInteractionSensor))]
    public sealed class FirstPersonPlayerController : MonoBehaviour
    {
        [SerializeField]
        private FirstPersonPlayerSettings _settings;

        [SerializeField]
        private CharacterController _characterController;

        [SerializeField]
        private FirstPersonInputSource _inputSource;

        [SerializeField]
        private Transform _cameraPivot;

        [SerializeField]
        private Camera _playerCamera;

        [SerializeField]
        private PlayerHoldItemAnchor _holdItemAnchor;

        [SerializeField]
        private PlayerInteractionSensor _interactionSensor;

        [SerializeField]
        private GameplayHudPresenter _hud;

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _yaw;
        private float _pitch;
        private float _currentStamina;
        private float _staminaRecoveryTimer;
        private bool _isInitialized;
        private bool _allowMovement = true;
        private bool _allowLook = true;
        private bool _allowInteraction = true;

        public Camera PlayerCamera => _playerCamera;

        public GameplayHudPresenter Hud => _hud;

        public PlayerHoldItemAnchor HoldItemAnchor => _holdItemAnchor;

        public PlayerInputFrame CurrentInputFrame { get; private set; }

        public void SetSettings(FirstPersonPlayerSettings value)
        {
            _settings = value;
        }

        public void SetHud(GameplayHudPresenter value)
        {
            _hud = value;
        }

        public void SetControlState(bool allowMovement, bool allowLook, bool allowInteraction)
        {
            _allowMovement = allowMovement;
            _allowLook = allowLook;
            _allowInteraction = allowInteraction;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            LockCursor();
        }

        private void OnDisable()
        {
            UnlockCursor();
        }

        private void Update()
        {
            EnsureInitialized();
            HandleCursorState();

            var deltaTime = Time.deltaTime;
            CurrentInputFrame = _inputSource.ReadFrame();

            PlayerInputFrame input = CurrentInputFrame;
            if (!_allowLook)
            {
                input = input.WithoutLook();
            }

            if (!_allowMovement)
            {
                input = input.WithoutMovement();
            }

            if (!_allowInteraction)
            {
                input = input.WithoutInteraction();
            }

            ApplyLook(input, deltaTime);
            UpdateMovement(input, deltaTime);
            UpdateInteraction(input);
            UpdateHud(input);
        }

        private void ResolveReferences()
        {
            if (_settings == null)
            {
                _settings = FirstPersonPlayerSettings.CreateRuntimeDefaults();
            }

            _characterController =
                _characterController != null
                    ? _characterController
                    : GetComponent<CharacterController>();
            _inputSource =
                _inputSource != null ? _inputSource : GetComponent<FirstPersonInputSource>();
            _interactionSensor =
                _interactionSensor != null
                    ? _interactionSensor
                    : GetComponent<PlayerInteractionSensor>();
            _playerCamera =
                _playerCamera != null ? _playerCamera : GetComponentInChildren<Camera>(true);

            if (_cameraPivot == null)
            {
                Transform pivot = transform.Find("CameraPivot");
                _cameraPivot =
                    pivot != null
                        ? pivot
                        : (_playerCamera != null ? _playerCamera.transform.parent : null);
            }

            _holdItemAnchor =
                _holdItemAnchor != null
                    ? _holdItemAnchor
                    : GetComponentInChildren<PlayerHoldItemAnchor>(true);
            _hud = _hud != null ? _hud : FindFirstObjectByType<GameplayHudPresenter>();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            ResolveReferences();

            _currentStamina = _settings.MaxStamina;
            _yaw = transform.eulerAngles.y;
            _pitch = _cameraPivot != null ? NormalizeAngle(_cameraPivot.localEulerAngles.x) : 0f;

            if (_characterController != null)
            {
                _characterController.height = _settings.StandingHeight;
                _characterController.center = new Vector3(0f, _settings.StandingHeight * 0.5f, 0f);
            }

            if (_cameraPivot != null)
            {
                Vector3 pivotPosition = _cameraPivot.localPosition;
                pivotPosition.y = _settings.StandingCameraHeight;
                _cameraPivot.localPosition = pivotPosition;
            }

            if (_interactionSensor != null)
            {
                _interactionSensor.Configure(
                    _playerCamera,
                    _settings.InteractionDistance,
                    _settings.InteractionLayers
                );
            }

            if (_hud != null)
            {
                _hud.SetInteractionPrompt(string.Empty);
                _hud.SetStamina(1f, false);
            }

            _isInitialized = true;
        }

        private void HandleCursorState()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
                return;
            }

            if (
                Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame
            )
            {
                LockCursor();
            }
        }

        private void ApplyLook(PlayerInputFrame input, float deltaTime)
        {
            var yawDelta = input.PointerLook.x * _settings.MouseLookSensitivity;
            yawDelta += input.StickLook.x * _settings.GamepadLookSpeed * deltaTime;

            var pitchDelta = input.PointerLook.y * _settings.MouseLookSensitivity;
            pitchDelta += input.StickLook.y * _settings.GamepadLookSpeed * deltaTime;

            _yaw += yawDelta;
            _pitch = Mathf.Clamp(_pitch - pitchDelta, -_settings.PitchLimit, _settings.PitchLimit);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (_cameraPivot != null)
            {
                _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void UpdateMovement(PlayerInputFrame input, float deltaTime)
        {
            var wantsToCrouch = input.CrouchHeld;
            if (!wantsToCrouch && !CanStandUp())
            {
                wantsToCrouch = true;
            }

            UpdateCharacterHeight(wantsToCrouch, deltaTime);

            Vector3 moveDirection =
                transform.right * input.Move.x + transform.forward * input.Move.y;
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            var isMoving = input.Move.sqrMagnitude > 0.01f;
            var canSprint = _currentStamina > 0.05f;
            var isSprinting = input.SprintHeld && !wantsToCrouch && isMoving && canSprint;

            var targetSpeed = _settings.WalkSpeed;
            if (wantsToCrouch)
            {
                targetSpeed = _settings.CrouchSpeed;
            }
            else if (isSprinting)
            {
                targetSpeed = _settings.SprintSpeed;
            }

            Vector3 targetVelocity = moveDirection * targetSpeed;
            var acceleration = _characterController.isGrounded
                ? _settings.Acceleration
                : _settings.AirAcceleration;
            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity,
                targetVelocity,
                acceleration * deltaTime
            );

            if (_characterController.isGrounded)
            {
                if (_verticalVelocity < _settings.GroundedSnapVelocity)
                {
                    _verticalVelocity = _settings.GroundedSnapVelocity;
                }

                if (input.JumpPressed && !wantsToCrouch)
                {
                    _verticalVelocity = Mathf.Sqrt(_settings.JumpHeight * -2f * _settings.Gravity);
                }
            }
            else
            {
                _verticalVelocity += _settings.Gravity * deltaTime;
            }

            Vector3 motion = _horizontalVelocity;
            motion.y = _verticalVelocity;

            CollisionFlags collisionFlags = _characterController.Move(motion * deltaTime);
            if ((collisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
            {
                _verticalVelocity = 0f;
            }

            if (
                _characterController.isGrounded
                && _verticalVelocity < _settings.GroundedSnapVelocity
            )
            {
                _verticalVelocity = _settings.GroundedSnapVelocity;
            }

            UpdateStamina(isSprinting, isMoving, deltaTime);
        }

        private void UpdateInteraction(PlayerInputFrame input)
        {
            if (_interactionSensor == null)
            {
                return;
            }

            var context = new InteractionContext(this, _holdItemAnchor, _hud, _playerCamera);
            _interactionSensor.Scan(context);

            if (input.InteractPressed)
            {
                _interactionSensor.TryInteract(context);
                _interactionSensor.Scan(context);
            }
        }

        private void UpdateHud(PlayerInputFrame input)
        {
            if (_hud == null)
            {
                return;
            }

            var context = new InteractionContext(this, _holdItemAnchor, _hud, _playerCamera);
            _hud.SetInteractionPrompt(
                _interactionSensor != null ? _interactionSensor.BuildPrompt(context) : string.Empty
            );

            var showStamina = input.SprintHeld || _currentStamina < _settings.MaxStamina - 0.01f;
            var normalizedStamina =
                _settings.MaxStamina > 0.001f ? _currentStamina / _settings.MaxStamina : 0f;
            _hud.SetStamina(normalizedStamina, showStamina);
        }

        private void UpdateStamina(bool isSprinting, bool isMoving, float deltaTime)
        {
            if (isSprinting && isMoving)
            {
                _currentStamina = Mathf.Max(
                    0f,
                    _currentStamina - _settings.SprintDrainPerSecond * deltaTime
                );
                _staminaRecoveryTimer = _settings.StaminaRegenDelay;
                return;
            }

            _staminaRecoveryTimer = Mathf.Max(0f, _staminaRecoveryTimer - deltaTime);
            if (_staminaRecoveryTimer > 0f)
            {
                return;
            }

            _currentStamina = Mathf.Min(
                _settings.MaxStamina,
                _currentStamina + _settings.StaminaRegenPerSecond * deltaTime
            );
        }

        private void UpdateCharacterHeight(bool crouched, float deltaTime)
        {
            var targetHeight = crouched ? _settings.CrouchingHeight : _settings.StandingHeight;
            var targetCameraHeight = crouched
                ? _settings.CrouchingCameraHeight
                : _settings.StandingCameraHeight;

            _characterController.height = Mathf.Lerp(
                _characterController.height,
                targetHeight,
                1f - Mathf.Exp(-_settings.HeightLerpSpeed * deltaTime)
            );
            _characterController.center = new Vector3(0f, _characterController.height * 0.5f, 0f);

            if (_cameraPivot == null)
            {
                return;
            }

            Vector3 localPosition = _cameraPivot.localPosition;
            localPosition.y = Mathf.Lerp(
                localPosition.y,
                targetCameraHeight,
                1f - Mathf.Exp(-_settings.HeightLerpSpeed * deltaTime)
            );
            _cameraPivot.localPosition = localPosition;
        }

        private bool CanStandUp()
        {
            if (_characterController == null)
            {
                return true;
            }

            var radius = Mathf.Max(0.01f, _characterController.radius * 0.95f);
            Vector3 bottom = transform.position + Vector3.up * radius;
            Vector3 top = transform.position + Vector3.up * (_settings.StandingHeight - radius);
            return !Physics.CheckCapsule(
                bottom,
                top,
                radius,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
            {
                angle -= 360f;
            }

            while (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
