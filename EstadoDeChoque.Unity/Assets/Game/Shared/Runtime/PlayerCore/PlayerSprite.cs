using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore
{
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Player/Sprite")]
    public sealed class PlayerSprite : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private PlayerMovement _movement;

        private SpriteRenderer _spriteRenderer;
        private bool _facingLeft;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_spriteRenderer == null || _movement == null)
            {
                return;
            }

            Vector3 velocity = _movement.Velocity;
            if (Mathf.Abs(velocity.x) > 0.01f)
            {
                _facingLeft = velocity.x < 0f;
                _spriteRenderer.flipX = _facingLeft;
            }
        }

        public void SetMovement(PlayerMovement movement)
        {
            _movement = movement;
        }

        public void SetSprite(Sprite sprite)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }
    }
}
