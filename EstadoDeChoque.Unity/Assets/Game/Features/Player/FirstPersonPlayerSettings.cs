using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Player
{
    [CreateAssetMenu(
        menuName = "Estado De Choque/Player/First Person Settings",
        fileName = "FirstPersonPlayerSettings"
    )]
    public sealed class FirstPersonPlayerSettings : ScriptableObject
    {
        [Header("Look")]
        public float MouseLookSensitivity = 0.12f;
        public float GamepadLookSpeed = 180f;
        public float PitchLimit = 85f;

        [Header("Movement")]
        public float WalkSpeed = 3.25f;
        public float SprintSpeed = 5.4f;
        public float CrouchSpeed = 1.8f;
        public float Acceleration = 18f;
        public float AirAcceleration = 6f;
        public float Gravity = -24f;
        public float JumpHeight = 1.15f;
        public float GroundedSnapVelocity = -2f;

        [Header("Stance")]
        public float StandingHeight = 1.78f;
        public float CrouchingHeight = 1.2f;
        public float StandingCameraHeight = 0.72f;
        public float CrouchingCameraHeight = 0.42f;
        public float HeightLerpSpeed = 12f;

        [Header("Interaction")]
        public float InteractionDistance = 2.5f;
        public LayerMask InteractionLayers = Physics.DefaultRaycastLayers;

        [Header("Stamina")]
        public float MaxStamina = 4.5f;
        public float SprintDrainPerSecond = 1.2f;
        public float StaminaRegenPerSecond = 0.9f;
        public float StaminaRegenDelay = 1.25f;

        public static FirstPersonPlayerSettings CreateRuntimeDefaults()
        {
            FirstPersonPlayerSettings runtimeSettings = CreateInstance<FirstPersonPlayerSettings>();
            runtimeSettings.name = "RuntimeFirstPersonPlayerSettings";
            runtimeSettings.hideFlags = HideFlags.HideAndDontSave;
            return runtimeSettings;
        }
    }
}
