using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Player
{
    public sealed class FirstPersonInputSource : MonoBehaviour
    {
        public PlayerInputFrame ReadFrame()
        {
            return new PlayerInputFrame(
                ReadMovement(),
                ReadPointerLook(),
                ReadStickLook(),
                WasPressedThisFrame(Keyboard.current?.spaceKey)
                    || WasPressedThisFrame(Gamepad.current?.buttonSouth),
                IsPressed(Keyboard.current?.leftShiftKey)
                    || IsPressed(Gamepad.current?.leftStickButton),
                IsPressed(Keyboard.current?.cKey) || IsPressed(Gamepad.current?.buttonEast),
                WasPressedThisFrame(Keyboard.current?.eKey)
                    || WasPressedThisFrame(Gamepad.current?.buttonNorth),
                WasPressedThisFrame(Mouse.current?.leftButton)
                    || WasPressedThisFrame(Gamepad.current?.buttonWest)
            );
        }

        private static Vector2 ReadMovement()
        {
            Vector2 keyboardMovement = Vector2.zero;

            if (IsPressed(Keyboard.current?.wKey) || IsPressed(Keyboard.current?.upArrowKey))
            {
                keyboardMovement.y += 1f;
            }

            if (IsPressed(Keyboard.current?.sKey) || IsPressed(Keyboard.current?.downArrowKey))
            {
                keyboardMovement.y -= 1f;
            }

            if (IsPressed(Keyboard.current?.dKey) || IsPressed(Keyboard.current?.rightArrowKey))
            {
                keyboardMovement.x += 1f;
            }

            if (IsPressed(Keyboard.current?.aKey) || IsPressed(Keyboard.current?.leftArrowKey))
            {
                keyboardMovement.x -= 1f;
            }

            Vector2 gamepadMovement =
                Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
            return Vector2.ClampMagnitude(keyboardMovement + gamepadMovement, 1f);
        }

        private static Vector2 ReadPointerLook()
        {
            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        }

        private static Vector2 ReadStickLook()
        {
            return Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
        }

        private static bool WasPressedThisFrame(ButtonControl button)
        {
            return button != null && button.wasPressedThisFrame;
        }

        private static bool IsPressed(ButtonControl button)
        {
            return button != null && button.isPressed;
        }
    }

    public readonly struct PlayerInputFrame
    {
        public PlayerInputFrame(
            Vector2 move,
            Vector2 pointerLook,
            Vector2 stickLook,
            bool jumpPressed,
            bool sprintHeld,
            bool crouchHeld,
            bool interactPressed,
            bool attackPressed
        )
        {
            Move = move;
            PointerLook = pointerLook;
            StickLook = stickLook;
            JumpPressed = jumpPressed;
            SprintHeld = sprintHeld;
            CrouchHeld = crouchHeld;
            InteractPressed = interactPressed;
            AttackPressed = attackPressed;
        }

        public Vector2 Move { get; }

        public Vector2 PointerLook { get; }

        public Vector2 StickLook { get; }

        public bool JumpPressed { get; }

        public bool SprintHeld { get; }

        public bool CrouchHeld { get; }

        public bool InteractPressed { get; }

        public bool AttackPressed { get; }

        public PlayerInputFrame WithoutMovement()
        {
            return new PlayerInputFrame(
                Vector2.zero,
                PointerLook,
                StickLook,
                false,
                false,
                false,
                InteractPressed,
                AttackPressed
            );
        }

        public PlayerInputFrame WithoutLook()
        {
            return new PlayerInputFrame(
                Move,
                Vector2.zero,
                Vector2.zero,
                JumpPressed,
                SprintHeld,
                CrouchHeld,
                InteractPressed,
                AttackPressed
            );
        }

        public PlayerInputFrame WithoutInteraction()
        {
            return new PlayerInputFrame(
                Move,
                PointerLook,
                StickLook,
                JumpPressed,
                SprintHeld,
                CrouchHeld,
                false,
                false
            );
        }
    }
}
