using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Player
{
    public sealed class PlayerHoldItemAnchor : MonoBehaviour
    {
        private HoldableItem _equippedItem;

        public bool HasItem => _equippedItem != null;

        public Transform AnchorTransform => transform;

        public bool TryEquip(HoldableItem item)
        {
            if (item == null)
            {
                return false;
            }

            if (_equippedItem != null && _equippedItem != item)
            {
                return false;
            }

            _equippedItem = item;
            item.AttachTo(AnchorTransform);
            return true;
        }
    }
}
