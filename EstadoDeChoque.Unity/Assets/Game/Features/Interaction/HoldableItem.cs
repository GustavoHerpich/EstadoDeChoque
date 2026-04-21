using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class HoldableItem : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string _itemName = "objeto";

        [SerializeField]
        private Vector3 _holdLocalPosition = new(0.1f, -0.15f, 0.28f);

        [SerializeField]
        private Vector3 _holdLocalEulerAngles = new(15f, -70f, 0f);

        private Collider[] _cachedColliders;
        private Rigidbody _cachedRigidbody;

        public void Configure(string displayName, Vector3 localPosition, Vector3 localEulerAngles)
        {
            _itemName = displayName;
            _holdLocalPosition = localPosition;
            _holdLocalEulerAngles = localEulerAngles;
        }

        public bool CanInteract(InteractionContext context)
        {
            return context.HoldItemAnchor != null && !context.HoldItemAnchor.HasItem;
        }

        public string GetInteractionPrompt(InteractionContext context)
        {
            if (context.HoldItemAnchor == null)
            {
                return string.Empty;
            }

            if (context.HoldItemAnchor.HasItem)
            {
                return "As maos ja estao ocupadas";
            }

            return "Pegar " + _itemName;
        }

        public void Interact(InteractionContext context)
        {
            if (context.HoldItemAnchor == null)
            {
                return;
            }

            if (context.HoldItemAnchor.HasItem)
            {
                context.ShowMessage("As maos ja estao ocupadas.", 1.25f);
                return;
            }

            if (context.HoldItemAnchor.TryEquip(this))
            {
                context.ShowMessage(_itemName + " equipado.", 1.25f);
            }
        }

        public void AttachTo(Transform anchor)
        {
            transform.SetParent(anchor, false);
            transform.SetLocalPositionAndRotation(
                _holdLocalPosition,
                Quaternion.Euler(_holdLocalEulerAngles)
            );

            _cachedRigidbody =
                _cachedRigidbody != null ? _cachedRigidbody : GetComponent<Rigidbody>();

            if (_cachedRigidbody != null)
            {
                _cachedRigidbody.isKinematic = true;
                _cachedRigidbody.detectCollisions = false;
            }

            _cachedColliders =
                _cachedColliders != null && _cachedColliders.Length > 0
                    ? _cachedColliders
                    : GetComponentsInChildren<Collider>(true);

            for (var index = 0; index < _cachedColliders.Length; index++)
            {
                _cachedColliders[index].enabled = false;
            }
        }
    }
}
