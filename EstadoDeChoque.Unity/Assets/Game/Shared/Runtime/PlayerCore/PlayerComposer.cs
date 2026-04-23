using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore
{
    public sealed class PlayerComposer : MonoBehaviour
    {
        [Header("Composed Components")]
        [SerializeField]
        private PlayerComposition _playerComposition;

        [Header("Integration")]
        [SerializeField]
        private PlayerInteractionSensor _interactionSensor;

        [SerializeField]
        private PlayerHoldItemAnchor _holdItemAnchor;

        [SerializeField]
        private GameplayHudPresenter _hud;

        public Camera PlayerCamera { get; private set; }
        public GameplayHudPresenter Hud => _hud;
        public PlayerHoldItemAnchor HoldItemAnchor => _holdItemAnchor;
        public PlayerInteractionSensor InteractionSensor => _interactionSensor;

        public float CurrentStamina => _playerComposition?.CurrentStamina ?? 0f;
        public Vector3 Velocity => _playerComposition?.Velocity ?? Vector3.zero;
        public bool IsGrounded => _playerComposition?.IsGrounded ?? false;

        public PlayerInputFrame CurrentInputFrame =>
            _playerComposition?.InputSource != null
                ? _playerComposition.InputSource.ReadFrame()
                : default;

        private void Awake()
        {
            ResolveReferences();
            SetupBridge();
        }

        private void ResolveReferences()
        {
            if (_playerComposition == null)
                TryGetComponent(out _playerComposition);

            if (_interactionSensor == null)
                TryGetComponent(out _interactionSensor);

            if (_holdItemAnchor == null)
                _holdItemAnchor = GetComponentInChildren<PlayerHoldItemAnchor>(true);

            if (_hud == null)
                _hud = FindFirstObjectByType<GameplayHudPresenter>();

            PlayerCamera = GetComponentInChildren<Camera>(true);
            if (PlayerCamera == null)
                PlayerCamera = FindFirstObjectByType<Camera>();
        }

        private void SetupBridge()
        {
            if (_playerComposition == null)
                return;

            PlayerMovement movement = _playerComposition.Movement;
            if (movement != null)
            {
                PlayerMovementStaminaBridge bridge =
                    movement.GetComponent<PlayerMovementStaminaBridge>()
                    ?? movement.gameObject.AddComponent<PlayerMovementStaminaBridge>();

                bridge.OnStaminaChanged += OnStaminaChanged;
            }

            FirstPersonPlayerSettings settings = _playerComposition.Settings;
            if (_interactionSensor != null && PlayerCamera != null && settings != null)
            {
                _interactionSensor.Configure(
                    PlayerCamera,
                    settings.InteractionDistance,
                    settings.InteractionLayers
                );
            }
        }

        private void OnStaminaChanged(float stamina)
        {
            if (_hud == null)
                return;

            FirstPersonPlayerSettings settings = _playerComposition?.Settings;
            float normalized =
                settings != null && settings.MaxStamina > 0.001f
                    ? stamina / settings.MaxStamina
                    : 0f;
            _hud.SetStamina(normalized, true);
        }

        public void SetHud(GameplayHudPresenter hud) => _hud = hud;

        public void SetSettings(FirstPersonPlayerSettings settings)
        {
            _playerComposition?.SetSettings(settings);
        }

        public void SetCamera(Camera camera)
        {
            PlayerCamera = camera;
            _playerComposition?.SetCamera(camera);
        }

        public void SetControlState(bool allowMovement, bool allowLook, bool allowInteraction)
        {
            _playerComposition?.SetControlState(allowMovement, allowLook);
            if (_interactionSensor != null)
                _interactionSensor.enabled = allowInteraction;
        }

        public InteractionContext CreateInteractionContext() =>
            new(_holdItemAnchor, _hud, PlayerCamera);

        private void Update()
        {
            // Process interaction only if sensor and HUD are available and sensor is enabled
            if (_interactionSensor == null || _hud == null || !_interactionSensor.enabled)
                return;

            // Ensure we have a camera
            if (PlayerCamera == null)
                return;

            // Build context and scan for interactable
            InteractionContext context = CreateInteractionContext();
            _interactionSensor.Scan(context);

            // Update interaction prompt in HUD
            _hud.SetInteractionPrompt(_interactionSensor.BuildPrompt(context));

            // Check for interact input
            PlayerInputFrame input = CurrentInputFrame;
            if (input.InteractPressed)
            {
                _interactionSensor.TryInteract(context);
                // Re-scan immediately after interaction to refresh state
                _interactionSensor.Scan(context);
            }
        }
    }

    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerMovementStaminaBridge : MonoBehaviour
    {
        public event System.Action<float> OnStaminaChanged;
        public event System.Action<bool> OnGroundedChanged;

        private PlayerMovement _movement;
        private float _lastStamina = float.MinValue;
        private bool _lastGrounded;
        private bool _firstUpdate = true;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            if (_movement == null)
                return;

            float stamina = _movement.Stamina;
            bool grounded = _movement.IsGrounded;

            if (_firstUpdate || !Mathf.Approximately(stamina, _lastStamina))
            {
                _lastStamina = stamina;
                OnStaminaChanged?.Invoke(stamina);
            }

            if (_firstUpdate || grounded != _lastGrounded)
            {
                _lastGrounded = grounded;
                OnGroundedChanged?.Invoke(grounded);
            }

            _firstUpdate = false;
        }
    }
}
