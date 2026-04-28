using System.Collections.Generic;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore
{
    public sealed class PlayerComposition : MonoBehaviour
    {
        private readonly List<PlayerMovement> _movements = new();
        private readonly List<PlayerLook> _looks = new();
        private readonly List<PlayerAnimation> _animations = new();
        private readonly List<PlayerSprite> _sprites = new();

        [SerializeField]
        private FirstPersonPlayerSettings _settings;

        [SerializeField]
        private FirstPersonInputSource _inputSource;

        [SerializeField]
        private CharacterController _characterController;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private Transform _cameraPivot;

        public PlayerMovement Movement => _movements.Count > 0 ? _movements[0] : null;
        public PlayerLook Look => _looks.Count > 0 ? _looks[0] : null;
        public PlayerAnimation Animation => _animations.Count > 0 ? _animations[0] : null;
        public PlayerSprite Sprite => _sprites.Count > 0 ? _sprites[0] : null;

        public float CurrentStamina => Movement?.Stamina ?? 0f;
        public Vector3 Velocity => Movement?.Velocity ?? Vector3.zero;
        public bool IsGrounded => Movement?.IsGrounded ?? false;
        public FirstPersonPlayerSettings Settings => _settings;
        public FirstPersonInputSource InputSource => _inputSource;

        public void SetSettings(FirstPersonPlayerSettings settings)
        {
            _settings = settings;
        }

        public void SetInput(FirstPersonInputSource input)
        {
            _inputSource = input;
        }

        private void Awake()
        {
            ResolveComponents();
            InitializeComponents();
        }

        private void ResolveComponents()
        {
            if (_settings == null)
            {
                _settings = FirstPersonPlayerSettings.CreateRuntimeDefaults();
            }

            if (_characterController == null)
            {
                TryGetComponent(out _characterController);
            }

            if (_inputSource == null)
            {
                TryGetComponent(out _inputSource);
            }

            GetComponents(_movements);
            GetComponents(_looks);
            GetComponents(_animations);
            GetComponents(_sprites);
        }

        private void InitializeComponents()
        {
            foreach (PlayerMovement movement in _movements)
            {
                movement.SetSettings(_settings);
                movement.SetInput(_inputSource);
            }

            // Auto-conecta Animation e Sprite ao primeiro Movement disponível
            foreach (PlayerAnimation animation in _animations)
            {
                PlayerMovement targetMovement = _movements.Count > 0 ? _movements[0] : null;
                animation.SetMovement(targetMovement);
            }

            foreach (PlayerSprite sprite in _sprites)
            {
                PlayerMovement targetMovement = _movements.Count > 0 ? _movements[0] : null;
                sprite.SetMovement(targetMovement);
            }
        }

        public void AddMovement(PlayerMovement component)
        {
            component.SetSettings(_settings);
            component.SetInput(_inputSource);
            _movements.Add(component);
        }

        public void AddLook(PlayerLook component)
        {
            component.SetSettings(_settings);
            component.SetInput(_inputSource);
            component.SetCamera(_camera);
            component.SetPivot(_cameraPivot);
            _looks.Add(component);
        }

        public void AddAnimation(PlayerAnimation component)
        {
            _animations.Add(component);
            // Connect to first movement if available
            if (_movements.Count > 0 && component != null)
            {
                component.SetMovement(_movements[0]);
            }
            // Movement must be added before animation/sprite for auto-connection to work
            Debug.Assert(
                _movements.Count > 0,
                "PlayerMovement should be added before PlayerAnimation for proper auto-connection."
            );
        }

        public void AddSprite(PlayerSprite component)
        {
            _sprites.Add(component);
            // Connect to first movement if available
            if (_movements.Count > 0 && component != null)
            {
                component.SetMovement(_movements[0]);
            }
            // Movement must be added before animation/sprite for auto-connection to work
            Debug.Assert(
                _movements.Count > 0,
                "PlayerMovement should be added before PlayerSprite for proper auto-connection."
            );
        }

        public void SetCamera(Camera camera)
        {
            _camera = camera;
            foreach (PlayerLook look in _looks)
            {
                look.SetCamera(camera);
            }
        }

        public void SetPivot(Transform pivot)
        {
            _cameraPivot = pivot;
            foreach (PlayerLook look in _looks)
            {
                look.SetPivot(pivot);
            }
        }

        public void SetControlState(bool enabledMovement, bool enabledLook)
        {
            if (Movement != null)
            {
                Movement.Enabled = enabledMovement;
            }

            if (Look != null)
            {
                Look.Enabled = enabledLook;
            }
        }
    }

    public sealed class PlayerCompositionBuilder
    {
        private readonly GameObject _gameObject;
        private FirstPersonPlayerSettings _settings;
        private FirstPersonInputSource _inputSource;
        private Camera _camera;
        private Transform _pivot;
        private bool _addMovement = true;
        private bool _addLook = true;
        private bool _addAnimation;
        private bool _addSprite;

        public PlayerCompositionBuilder(GameObject target)
        {
            _gameObject = target;
        }

        public PlayerCompositionBuilder WithSettings(FirstPersonPlayerSettings settings)
        {
            _settings = settings;
            return this;
        }

        public PlayerCompositionBuilder WithInput(FirstPersonInputSource input)
        {
            _inputSource = input;
            return this;
        }

        public PlayerCompositionBuilder WithCamera(Camera camera)
        {
            _camera = camera;
            return this;
        }

        public PlayerCompositionBuilder WithPivot(Transform pivot)
        {
            _pivot = pivot;
            return this;
        }

        public PlayerCompositionBuilder WithoutMovement()
        {
            _addMovement = false;
            return this;
        }

        public PlayerCompositionBuilder WithoutLook()
        {
            _addLook = false;
            return this;
        }

        public PlayerCompositionBuilder WithAnimation()
        {
            _addAnimation = true;
            return this;
        }

        public PlayerCompositionBuilder WithSprite()
        {
            _addSprite = true;
            return this;
        }

        public PlayerComposition Build()
        {
            if (_inputSource == null && (_addMovement || _addLook))
            {
                _inputSource = _gameObject.AddComponent<FirstPersonInputSource>();
            }

            PlayerComposition composition = _gameObject.AddComponent<PlayerComposition>();
            composition.SetSettings(_settings);
            composition.SetInput(_inputSource);
            if (_camera != null)
                composition.SetCamera(_camera);
            if (_pivot != null)
                composition.SetPivot(_pivot);

            if (_addMovement)
            {
                PlayerMovement movement = _gameObject.AddComponent<PlayerMovement>();
                composition.AddMovement(movement);
            }

            if (_addLook)
            {
                PlayerLook look = _gameObject.AddComponent<PlayerLook>();
                composition.AddLook(look);
            }

            if (_addAnimation)
            {
                PlayerAnimation animation = _gameObject.AddComponent<PlayerAnimation>();
                composition.AddAnimation(animation);
            }

            if (_addSprite)
            {
                PlayerSprite sprite = _gameObject.AddComponent<PlayerSprite>();
                composition.AddSprite(sprite);
            }

            return composition;
        }
    }
}
