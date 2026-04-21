using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Hospital
{
    public sealed class HospitalNpcInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string _displayName = "Thomas";

        [SerializeField]
        private string _promptText = "Falar com Thomas";

        [SerializeField]
        private bool _isAvailable;

        private HospitalIntroFlowController _flowController;

        public string DisplayName => _displayName;

        public void Configure(
            HospitalIntroFlowController flowController,
            string displayName,
            string promptText
        )
        {
            _flowController = flowController;
            _displayName = displayName;
            _promptText = promptText;
        }

        public void SetAvailability(bool isAvailable)
        {
            _isAvailable = isAvailable;
        }

        public bool CanInteract(InteractionContext context)
        {
            return _isAvailable && _flowController != null;
        }

        public string GetInteractionPrompt(InteractionContext context)
        {
            return _isAvailable ? _promptText : string.Empty;
        }

        public void Interact(InteractionContext context)
        {
            if (!CanInteract(context))
            {
                return;
            }

            _flowController.HandleNpcInteracted(this);
        }
    }
}
