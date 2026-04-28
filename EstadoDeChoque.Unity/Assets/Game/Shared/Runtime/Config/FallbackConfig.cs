using System;
using UnityEngine;
using static EstadoDeChoque.Gameplay.Assets.Game.Features.Cutscenes.CutsceneSequence;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Config
{
    [Serializable]
    public struct FallbackConfig
    {
        public Camera CutsceneCamera;
        public CameraShot[] CameraShots;
        public SubtitleCue[] SubtitleCues;

        [Header("Fade")]
        public bool FadeInFromBlack;
        public float FadeInDuration;
        public bool FadeOutToBlack;
        public float FadeOutDuration;

        [Header("Playback")]
        public bool AllowSkip;
        public float ShotBlendDuration;
        public float FallbackDuration;

        [Header("Framing")]
        public bool ShowLetterbox;

        [Range(0.05f, 0.25f)]
        public float LetterboxHeightNormalized;

        [Header("Control Lock")]
        public bool LockMovement;
        public bool LockLook;
        public bool LockInteraction;
        public static FallbackConfig Default =>
            new()
            {
                AllowSkip = true,
                ShotBlendDuration = 0.75f,
                FallbackDuration = 2f,
                ShowLetterbox = true,
                LetterboxHeightNormalized = 0.12f,
                LockMovement = true,
                LockLook = true,
                LockInteraction = true,
                FadeInDuration = 0.8f,
                FadeOutDuration = 0.8f,
            };
    }
}
