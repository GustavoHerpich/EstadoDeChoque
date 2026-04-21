using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction
{
    public interface IInteractable
    {
        bool CanInteract(InteractionContext context);

        string GetInteractionPrompt(InteractionContext context);

        void Interact(InteractionContext context);
    }

    public readonly struct InteractionContext
    {
        public InteractionContext(
            FirstPersonPlayerController player,
            PlayerHoldItemAnchor holdItemAnchor,
            GameplayHudPresenter hud,
            Camera playerCamera
        )
        {
            Player = player;
            HoldItemAnchor = holdItemAnchor;
            Hud = hud;
            PlayerCamera = playerCamera;
        }

        public FirstPersonPlayerController Player { get; }

        public PlayerHoldItemAnchor HoldItemAnchor { get; }

        public GameplayHudPresenter Hud { get; }

        public Camera PlayerCamera { get; }

        public void ShowMessage(string message, float duration = 2f)
        {
            if (Hud != null)
            {
                Hud.ShowMessage(message, duration);
            }
        }
    }
}
