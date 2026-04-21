using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Hospital
{
    public sealed class BedRestInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string _promptText = "Deitar novamente";

        [SerializeField]
        private bool _isAvailable;

        private HospitalIntroFlowController _flowController;

        public void Configure(HospitalIntroFlowController flowController, string promptText)
        {
            _flowController = flowController;
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

            _flowController.HandleBedInteracted(this);
        }
    }
}
