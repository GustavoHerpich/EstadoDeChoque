using System;
using System.Collections;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore;
using UnityEngine;
using UnityEngine.Playables;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Cutscenes
{
    /// <summary>
    /// Reproduz sequências de cutscene (<see cref="CutsceneSequence"/>), gerenciando
    /// câmera, fade, letterbox, legenda e controle do jogador durante a reprodução.
    /// </summary>
    public sealed class CutscenePlayer : MonoBehaviour
    {
        [Tooltip("Compositor do jogador. Necessário para bloquear controle durante cutscenes.")]
        [SerializeField]
        private PlayerComposer _player;

        [Tooltip("HUD de gameplay. Usado para fade, letterbox e legendas.")]
        [SerializeField]
        private GameplayHudPresenter _hud;

        private Coroutine _playRoutine;

        /// <summary>Retorna true se uma cutscene está sendo reproduzida.</summary>
        public bool IsPlaying => _playRoutine != null;

        /// <summary>
        /// Configura as dependências do player via código.
        /// </summary>
        public void Configure(PlayerComposer player, GameplayHudPresenter hud)
        {
            _player = player;
            _hud = hud;
        }

        /// <summary>
        /// Tenta iniciar a reprodução de uma cutscene. Retorna false se já houver uma em andamento
        /// ou se os parâmetros obrigatórios forem nulos.
        /// </summary>
        public bool TryPlay(CutsceneSequence cutsceneSequence, Action onCompleted = null)
        {
            if (IsPlaying || cutsceneSequence == null || _player == null || _hud == null)
                return false;

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
                yield return _hud.Fade(1f, 0f, cutsceneSequence.FadeInDuration);
            }
            else
            {
                _hud.SetFade(0f);
            }

            if (cutsceneSequence.UsesTimeline)
                yield return PlayTimelineCutscene(cutsceneSequence);
            else
                yield return PlayFallbackCutscene(cutsceneSequence, cutsceneCamera);

            _hud.HideSubtitle();

            if (cutsceneSequence.FadeOutToBlack)
                yield return _hud.Fade(0f, 1f, cutsceneSequence.FadeOutDuration);
            else
                _hud.SetFade(0f);

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
                yield break;

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
            CutsceneSequence.CameraShot[] shots = cutsceneSequence.CameraShots;
            if (cutsceneCamera == null || shots.Length == 0)
            {
                yield return WaitWithSubtitles(
                    cutsceneSequence,
                    cutsceneSequence.GetPlaybackDuration()
                );
                yield break;
            }

            Transform cameraTransform = cutsceneCamera.transform;
            if (shots[0].Viewpoint != null)
                ApplyTransform(cameraTransform, shots[0].Viewpoint);

            float elapsedTime = 0f;

            for (int i = 0; i < shots.Length; i++)
            {
                Transform shotTransform = shots[i].Viewpoint;
                if (shotTransform != null && i > 0)
                {
                    Transform prevShot = shots[i - 1].Viewpoint;
                    if (prevShot != null)
                    {
                        float blendDuration = cutsceneSequence.ShotBlendDuration;
                        float blendElapsed = 0f;

                        while (blendElapsed < blendDuration)
                        {
                            if (cutsceneSequence.AllowSkip && WasSkipPressed())
                                yield break;

                            blendElapsed += Time.deltaTime;
                            elapsedTime += Time.deltaTime;

                            float t =
                                blendDuration > 0.001f
                                    ? Mathf.Clamp01(blendElapsed / blendDuration)
                                    : 1f;
                            cameraTransform.SetPositionAndRotation(
                                Vector3.Lerp(prevShot.position, shotTransform.position, t),
                                Quaternion.Slerp(prevShot.rotation, shotTransform.rotation, t)
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

                float holdElapsed = 0f;
                while (holdElapsed < shots[i].HoldDuration)
                {
                    if (cutsceneSequence.AllowSkip && WasSkipPressed())
                        yield break;

                    holdElapsed += Time.deltaTime;
                    elapsedTime += Time.deltaTime;
                    UpdateSubtitle(cutsceneSequence, elapsedTime);
                    yield return null;
                }
            }

            float totalDuration = cutsceneSequence.GetPlaybackDuration();
            if (elapsedTime < totalDuration)
                yield return WaitWithSubtitles(
                    cutsceneSequence,
                    totalDuration - elapsedTime,
                    elapsedTime
                );
        }

        private IEnumerator WaitWithSubtitles(
            CutsceneSequence seq,
            float duration,
            float initialElapsed = 0f
        )
        {
            float elapsed = initialElapsed;
            float waitElapsed = 0f;

            while (waitElapsed < duration)
            {
                if (seq.AllowSkip && WasSkipPressed())
                    yield break;

                waitElapsed += Time.deltaTime;
                elapsed += Time.deltaTime;
                UpdateSubtitle(seq, elapsed);
                yield return null;
            }
        }

        private void UpdateSubtitle(CutsceneSequence seq, float elapsedTime)
        {
            if (seq.TryGetSubtitle(elapsedTime, out var cue))
                _hud.ShowSubtitle(cue.Speaker, cue.Text);
            else
                _hud.HideSubtitle();
        }

        private bool WasSkipPressed()
        {
            PlayerInputFrame input = _player.CurrentInputFrame;
            return input.InteractPressed || input.AttackPressed || input.JumpPressed;
        }

        private static void ApplyTransform(Transform target, Transform source) =>
            target.SetPositionAndRotation(source.position, source.rotation);

        private static void SetCameraEnabled(Camera cam, bool enabled)
        {
            if (cam != null)
                cam.enabled = enabled;
        }
    }
}
