using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Hospital
{
    public sealed class MedicalChartInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string _promptText = "Ler ficha medica de Dante";

        [SerializeField]
        private string _documentTitle = "Ficha medica de Dante";

        [SerializeField]
        [TextArea(6, 16)]
        private string _documentBody = "Ficha medica temporaria.";

        [SerializeField]
        private bool _isAvailable = true;

        private HospitalIntroFlowController _flowController;

        public string DocumentTitle => _documentTitle;

        public string DocumentBody => _documentBody;

        public void Configure(
            HospitalIntroFlowController flowController,
            string promptText,
            string documentTitle,
            string documentBody
        )
        {
            _flowController = flowController;
            _promptText = promptText;
            _documentTitle = documentTitle;
            _documentBody = documentBody;
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

            _flowController.HandleMedicalChartInteracted(this);
        }
    }
}
