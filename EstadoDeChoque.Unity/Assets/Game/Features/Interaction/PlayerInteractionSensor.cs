using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction
{
    public sealed class PlayerInteractionSensor : MonoBehaviour
    {
        [SerializeField]
        private Camera _viewCamera;

        [SerializeField]
        private float _interactionDistance = 2.5f;

        [SerializeField]
        private LayerMask _interactionLayers = Physics.DefaultRaycastLayers;

        private IInteractable _currentInteractable;

        public void Configure(Camera targetCamera, float distance, LayerMask layers)
        {
            _viewCamera = targetCamera;
            _interactionDistance = distance;
            _interactionLayers = layers;
        }

        public void Scan(InteractionContext context)
        {
            _currentInteractable = null;
            if (_viewCamera == null)
            {
                return;
            }

            Ray ray = _viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (
                !Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    _interactionDistance,
                    _interactionLayers,
                    QueryTriggerInteraction.Collide
                )
            )
            {
                return;
            }

            _currentInteractable = ResolveInteractable(hit.collider);
        }

        public string BuildPrompt(InteractionContext context)
        {
            if (_currentInteractable == null)
            {
                return string.Empty;
            }

            return _currentInteractable.GetInteractionPrompt(context);
        }

        public void TryInteract(InteractionContext context)
        {
            if (_currentInteractable == null)
            {
                return;
            }

            if (!_currentInteractable.CanInteract(context))
            {
                return;
            }

            _currentInteractable.Interact(context);
        }

        private static IInteractable ResolveInteractable(Collider targetCollider)
        {
            MonoBehaviour[] behaviours = targetCollider.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }
    }
}
