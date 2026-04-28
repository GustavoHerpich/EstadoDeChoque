using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction
{
    public sealed class SimpleInteractionMessage : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string _promptText = "Inspecionar";

        [SerializeField]
        private string _messageText = "Mensagem de interacao temporaria.";

        [SerializeField]
        private bool _repeatable = true;

        private bool _wasConsumed;

        public void Configure(string prompt, string message)
        {
            _promptText = prompt;
            _messageText = message;
        }

        public bool CanInteract(InteractionContext context)
        {
            return _repeatable || !_wasConsumed;
        }

        public string GetInteractionPrompt(InteractionContext context)
        {
            return CanInteract(context) ? _promptText : string.Empty;
        }

        public void Interact(InteractionContext context)
        {
            if (!CanInteract(context))
            {
                return;
            }

            context.ShowMessage(_messageText, 4f);
            _wasConsumed = true;
        }
    }
}
