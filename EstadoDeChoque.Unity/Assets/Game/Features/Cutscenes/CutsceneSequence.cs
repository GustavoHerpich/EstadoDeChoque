using System;
using UnityEngine;
using UnityEngine.Playables;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Cutscenes
{
    public sealed class CutsceneSequence : MonoBehaviour
    {
        [Serializable]
        public struct CameraShot
        {
            [SerializeField]
            private Transform _viewpoint;

            [SerializeField]
            [Min(0f)]
            private readonly float _holdDuration;

            public CameraShot(Transform viewpoint, float holdDuration)
            {
                _viewpoint = viewpoint;
                _holdDuration = holdDuration;
            }

            public readonly Transform Viewpoint => _viewpoint;

            public readonly float HoldDuration => Mathf.Max(0f, _holdDuration);
        }

        [Serializable]
        public struct SubtitleCue
        {
            [SerializeField]
            [Min(0f)]
            private float _startTime;

            [SerializeField]
            [Min(0.1f)]
            private float _duration;

            [SerializeField]
            private string _speaker;

            [SerializeField]
            [TextArea(2, 5)]
            private string _text;

            public SubtitleCue(float startTime, float duration, string speaker, string text)
            {
                _startTime = Mathf.Max(0f, startTime);
                _duration = Mathf.Max(0.1f, duration);
                _speaker = speaker;
                _text = text;
            }

            public readonly float StartTime => _startTime;

            public readonly float Duration => Mathf.Max(0.1f, _duration);

            public readonly string Speaker => _speaker;

            public readonly string Text => _text;

            public readonly float EndTime => StartTime + Duration;

            public readonly bool IsActive(float time)
            {
                return time >= StartTime && time <= EndTime;
            }
        }

        [SerializeField]
        private PlayableDirector _timelineDirector;

        [SerializeField]
        private Camera _cutsceneCamera;

        [SerializeField]
        private bool _allowSkip = true;

        [SerializeField]
        private bool _showLetterbox = true;

        [SerializeField]
        [Range(0.05f, 0.25f)]
        private float _letterboxHeightNormalized = 0.12f;

        [SerializeField]
        private bool _fadeInFromBlack = true;

        [SerializeField]
        [Min(0f)]
        private float _fadeInDuration = 0.8f;

        [SerializeField]
        private bool _fadeOutToBlack;

        [SerializeField]
        [Min(0f)]
        private float _fadeOutDuration = 0.8f;

        [SerializeField]
        private bool _lockMovement = true;

        [SerializeField]
        private bool _lockLook = true;

        [SerializeField]
        private bool _lockInteraction = true;

        [SerializeField]
        [Min(0f)]
        private float _shotBlendDuration = 0.75f;

        [SerializeField]
        [Min(0f)]
        private float _fallbackDuration = 2f;

        [SerializeField]
        private CameraShot[] _cameraShots = Array.Empty<CameraShot>();

        [SerializeField]
        private SubtitleCue[] _subtitleCues = Array.Empty<SubtitleCue>();

        public PlayableDirector TimelineDirector => _timelineDirector;

        public Camera CutsceneCamera => _cutsceneCamera;

        public bool AllowSkip => _allowSkip;

        public bool ShowLetterbox => _showLetterbox;

        public float LetterboxHeightNormalized => _letterboxHeightNormalized;

        public bool FadeInFromBlack => _fadeInFromBlack;

        public float FadeInDuration => Mathf.Max(0f, _fadeInDuration);

        public bool FadeOutToBlack => _fadeOutToBlack;

        public float FadeOutDuration => Mathf.Max(0f, _fadeOutDuration);

        public bool LockMovement => _lockMovement;

        public bool LockLook => _lockLook;

        public bool LockInteraction => _lockInteraction;

        public float ShotBlendDuration => Mathf.Max(0f, _shotBlendDuration);

        public CameraShot[] CameraShots => _cameraShots ?? Array.Empty<CameraShot>();

        public bool UsesTimeline =>
            _timelineDirector != null && _timelineDirector.playableAsset != null;

        public void ConfigureFallback(
            Camera cutsceneCamera,
            CameraShot[] cameraShots,
            SubtitleCue[] subtitleCues,
            bool fadeInFromBlack,
            float fadeInDuration,
            bool fadeOutToBlack,
            float fadeOutDuration,
            bool allowSkip = true,
            float shotBlendDuration = 0.75f,
            float fallbackDuration = 2f,
            bool showLetterbox = true,
            float letterboxHeightNormalized = 0.12f,
            bool lockMovement = true,
            bool lockLook = true,
            bool lockInteraction = true
        )
        {
            _timelineDirector = null;
            _cutsceneCamera = cutsceneCamera;
            _cameraShots = cameraShots ?? Array.Empty<CameraShot>();
            _subtitleCues = subtitleCues ?? Array.Empty<SubtitleCue>();
            _fadeInFromBlack = fadeInFromBlack;
            _fadeInDuration = fadeInDuration;
            _fadeOutToBlack = fadeOutToBlack;
            _fadeOutDuration = fadeOutDuration;
            _allowSkip = allowSkip;
            _shotBlendDuration = shotBlendDuration;
            _fallbackDuration = fallbackDuration;
            _showLetterbox = showLetterbox;
            _letterboxHeightNormalized = letterboxHeightNormalized;
            _lockMovement = lockMovement;
            _lockLook = lockLook;
            _lockInteraction = lockInteraction;
        }

        public float GetPlaybackDuration()
        {
            if (UsesTimeline)
            {
                return Mathf.Max(0.01f, (float)_timelineDirector.duration);
            }

            var latestSubtitleEnd = 0f;
            for (var index = 0; index < _subtitleCues.Length; index++)
            {
                latestSubtitleEnd = Mathf.Max(latestSubtitleEnd, _subtitleCues[index].EndTime);
            }

            var shotDuration = 0f;
            if (_cameraShots != null && _cameraShots.Length > 0)
            {
                for (var index = 0; index < _cameraShots.Length; index++)
                {
                    shotDuration += _cameraShots[index].HoldDuration;
                }

                shotDuration += Mathf.Max(0, _cameraShots.Length - 1) * ShotBlendDuration;
            }

            return Mathf.Max(0.1f, _fallbackDuration, latestSubtitleEnd, shotDuration);
        }

        public bool TryGetSubtitle(float time, out SubtitleCue subtitleCue)
        {
            for (var index = 0; index < _subtitleCues.Length; index++)
            {
                if (_subtitleCues[index].IsActive(time))
                {
                    subtitleCue = _subtitleCues[index];
                    return true;
                }
            }

            subtitleCue = default;
            return false;
        }
    }
}
