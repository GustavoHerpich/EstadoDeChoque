using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore
{
    [AddComponentMenu("Player/Look")]
    public sealed class PlayerLook : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private FirstPersonPlayerSettings _settings;

        [SerializeField]
        private FirstPersonInputSource _inputSource;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private Transform _pivot;

        private float _yaw;
        private float _pitch;
        private bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        private void Start()
        {
            _yaw = transform.eulerAngles.y;
            if (_pivot != null)
            {
                _pitch = NormalizeAngle(_pivot.localEulerAngles.x);
            }
        }

        private void Update()
        {
            if (!_enabled || _inputSource == null || _settings == null)
            {
                return;
            }

            PlayerInputFrame input = _inputSource.ReadFrame();

            var yawDelta = input.PointerLook.x * _settings.MouseLookSensitivity;
            yawDelta += input.StickLook.x * _settings.GamepadLookSpeed * Time.deltaTime;

            var pitchDelta = input.PointerLook.y * _settings.MouseLookSensitivity;
            pitchDelta += input.StickLook.y * _settings.GamepadLookSpeed * Time.deltaTime;

            _yaw += yawDelta;
            _pitch = Mathf.Clamp(_pitch - pitchDelta, -_settings.PitchLimit, _settings.PitchLimit);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (_pivot != null)
            {
                _pivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
                angle -= 360f;
            while (angle < -180f)
                angle += 360f;
            return angle;
        }

        public void SetSettings(FirstPersonPlayerSettings settings)
        {
            _settings = settings;
        }

        public void SetInput(FirstPersonInputSource input)
        {
            _inputSource = input;
        }

        public void SetCamera(Camera camera)
        {
            _camera = camera;
        }

        public void SetPivot(Transform pivot)
        {
            _pivot = pivot;
        }
    }
}
