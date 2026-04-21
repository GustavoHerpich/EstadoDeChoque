using System;
using System.Collections;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Cutscenes
{
    public sealed class CutscenePlayer : MonoBehaviour
    {
        [SerializeField]
        private FirstPersonPlayerController _player;

        [SerializeField]
        private GameplayHudPresenter _hud;

        private Coroutine _playRoutine;

        public bool IsPlaying => _playRoutine != null;

        public void Configure(FirstPersonPlayerController player, GameplayHudPresenter hud)
        {
            _player = player;
            _hud = hud;
        }

        public bool TryPlay(CutsceneSequence cutsceneSequence, Action onCompleted = null)
        {
            if (IsPlaying || cutsceneSequence == null || _player == null || _hud == null)
            {
                return false;
            }

            _playRoutine = StartCoroutine(PlayRoutine(cutsceneSequence, onCompleted));
            return true;
        }

        private IEnumerator PlayRoutine(CutsceneSequence cutsceneSequence, Action onCompleted)
        {
            Camera playerCamera = _player.PlayerCamera;
            Camera cutsceneCamera = cutsceneSequence.CutsceneCamera;
            bool playerCameraWasEnabled = playerCamera != null && playerCamera.enabled;
            bool cutsceneCameraWasEnabled = cutsceneCamera != null && cutsceneCamera.enabled;

            _player.SetControlState(
                !cutsceneSequence.LockMovement,
                !cutsceneSequence.LockLook,
                !cutsceneSequence.LockInteraction
            );
            _hud.HideModal();
            _hud.SetLetterbox(
                cutsceneSequence.ShowLetterbox,
                cutsceneSequence.LetterboxHeightNormalized
            );

            if (cutsceneCamera != null)
            {
                SetCameraEnabled(playerCamera, false);
                SetCameraEnabled(cutsceneCamera, true);
            }

            if (cutsceneSequence.FadeInFromBlack)
            {
                _hud.SetFade(1f);
                yield return Fade(1f, 0f, cutsceneSequence.FadeInDuration);
            }
            else
            {
                _hud.SetFade(0f);
            }

            if (cutsceneSequence.UsesTimeline)
            {
                yield return PlayTimelineCutscene(cutsceneSequence);
            }
            else
            {
                yield return PlayFallbackCutscene(cutsceneSequence, cutsceneCamera);
            }

            _hud.HideSubtitle();

            if (cutsceneSequence.FadeOutToBlack)
            {
                yield return Fade(0f, 1f, cutsceneSequence.FadeOutDuration);
            }
            else
            {
                _hud.SetFade(0f);
            }

            _hud.SetLetterbox(false, cutsceneSequence.LetterboxHeightNormalized);

            if (cutsceneCamera != null)
            {
                SetCameraEnabled(cutsceneCamera, cutsceneCameraWasEnabled);
                SetCameraEnabled(playerCamera, playerCameraWasEnabled);
            }

            _player.SetControlState(true, true, true);
            _playRoutine = null;
            onCompleted?.Invoke();
        }

        private IEnumerator PlayTimelineCutscene(CutsceneSequence cutsceneSequence)
        {
            PlayableDirector director = cutsceneSequence.TimelineDirector;
            if (director == null)
            {
                yield break;
            }

            director.time = 0d;
            director.Evaluate();
            director.Play();

            while (director.state == PlayState.Playing)
            {
                float elapsedTime = (float)director.time;
                UpdateSubtitle(cutsceneSequence, elapsedTime);

                if (cutsceneSequence.AllowSkip && WasSkipPressed())
                {
                    director.Stop();
                    break;
                }

                yield return null;
            }

            director.Stop();
            UpdateSubtitle(cutsceneSequence, cutsceneSequence.GetPlaybackDuration());
        }

        private IEnumerator PlayFallbackCutscene(
            CutsceneSequence cutsceneSequence,
            Camera cutsceneCamera
        )
        {
            CutsceneSequence.CameraShot[] cameraShots = cutsceneSequence.CameraShots;
            if (cutsceneCamera == null || cameraShots.Length == 0)
            {
                yield return WaitWithSubtitles(
                    cutsceneSequence,
                    cutsceneSequence.GetPlaybackDuration()
                );
                yield break;
            }

            Transform cameraTransform = cutsceneCamera.transform;
            Transform firstShot = cameraShots[0].Viewpoint;
            if (firstShot != null)
            {
                ApplyTransform(cameraTransform, firstShot);
            }

            float elapsedTime = 0f;

            for (var index = 0; index < cameraShots.Length; index++)
            {
                Transform shotTransform = cameraShots[index].Viewpoint;
                if (shotTransform != null && index > 0)
                {
                    Transform previousShot = cameraShots[index - 1].Viewpoint;
                    if (previousShot != null)
                    {
                        float blendDuration = cutsceneSequence.ShotBlendDuration;
                        float blendElapsed = 0f;

                        while (blendElapsed < blendDuration)
                        {
                            if (cutsceneSequence.AllowSkip && WasSkipPressed())
                            {
                                yield break;
                            }

                            blendElapsed += Time.deltaTime;
                            elapsedTime += Time.deltaTime;

                            float t =
                                blendDuration > 0.001f
                                    ? Mathf.Clamp01(blendElapsed / blendDuration)
                                    : 1f;

                            cameraTransform.SetPositionAndRotation(
                                Vector3.Lerp(previousShot.position, shotTransform.position, t),
                                Quaternion.Slerp(previousShot.rotation, shotTransform.rotation, t)
                            );
                            UpdateSubtitle(cutsceneSequence, elapsedTime);
                            yield return null;
                        }
                    }
                    else
                    {
                        ApplyTransform(cameraTransform, shotTransform);
                    }
                }

                float holdDuration = cameraShots[index].HoldDuration;
                float holdElapsed = 0f;
                while (holdElapsed < holdDuration)
                {
                    if (cutsceneSequence.AllowSkip && WasSkipPressed())
                    {
                        yield break;
                    }

                    float deltaTime = Time.deltaTime;
                    holdElapsed += deltaTime;
                    elapsedTime += deltaTime;
                    UpdateSubtitle(cutsceneSequence, elapsedTime);
                    yield return null;
                }
            }

            float totalDuration = cutsceneSequence.GetPlaybackDuration();
            if (elapsedTime < totalDuration)
            {
                yield return WaitWithSubtitles(
                    cutsceneSequence,
                    totalDuration - elapsedTime,
                    elapsedTime
                );
            }
        }

        private IEnumerator WaitWithSubtitles(
            CutsceneSequence cutsceneSequence,
            float duration,
            float initialElapsed = 0f
        )
        {
            float elapsedTime = initialElapsed;
            float waitElapsed = 0f;

            while (waitElapsed < duration)
            {
                if (cutsceneSequence.AllowSkip && WasSkipPressed())
                {
                    yield break;
                }

                float deltaTime = Time.deltaTime;
                waitElapsed += deltaTime;
                elapsedTime += deltaTime;
                UpdateSubtitle(cutsceneSequence, elapsedTime);
                yield return null;
            }
        }

        private void UpdateSubtitle(CutsceneSequence cutsceneSequence, float elapsedTime)
        {
            if (cutsceneSequence.TryGetSubtitle(elapsedTime, out var subtitleCue))
            {
                _hud.ShowSubtitle(subtitleCue.Speaker, subtitleCue.Text);
                return;
            }

            _hud.HideSubtitle();
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration > 0.001f ? Mathf.Clamp01(elapsed / duration) : 1f;
                _hud.SetFade(Mathf.Lerp(fromAlpha, toAlpha, t));
                yield return null;
            }

            _hud.SetFade(toAlpha);
        }

        private bool WasSkipPressed()
        {
            PlayerInputFrame input = _player.CurrentInputFrame;
            return input.InteractPressed || input.AttackPressed || input.JumpPressed;
        }

        private static void ApplyTransform(Transform target, Transform source)
        {
            target.SetPositionAndRotation(source.position, source.rotation);
        }

        private static void SetCameraEnabled(Camera targetCamera, bool enabled)
        {
            if (targetCamera != null)
            {
                targetCamera.enabled = enabled;
            }
        }
    }
}
