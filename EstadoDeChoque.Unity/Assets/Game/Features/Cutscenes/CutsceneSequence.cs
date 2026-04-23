using System;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Config;
using UnityEngine;
using UnityEngine.Playables;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Cutscenes
{
    /// <summary>
    /// Define os dados de uma sequência de cutscene: câmera, shots, legendas, fade e controles do jogador.
    /// Suporta reprodução via Timeline ou fallback por câmera com shots sequenciais.
    /// </summary>
    public sealed class CutsceneSequence : MonoBehaviour
    {
        /// <summary>Representa um plano de câmera com duração de hold para cutscenes sem Timeline.</summary>
        [Serializable]
        public struct CameraShot
        {
            [Tooltip("Transform usado como posição e rotação da câmera neste plano.")]
            [SerializeField]
            private Transform _viewpoint;

            [Tooltip("Tempo em segundos que a câmera permanece neste plano antes de transicionar.")]
            [SerializeField]
            [Min(0f)]
            private float _holdDuration;

            public CameraShot(Transform viewpoint, float holdDuration)
            {
                _viewpoint = viewpoint;
                _holdDuration = holdDuration;
            }

            /// <summary>Transform de referência da câmera neste plano.</summary>
            public readonly Transform Viewpoint => _viewpoint;

            /// <summary>Duração de hold do plano em segundos (mínimo 0).</summary>
            public readonly float HoldDuration => Mathf.Max(0f, _holdDuration);
        }

        /// <summary>Define uma entrada de legenda sincronizada ao tempo de reprodução da cutscene.</summary>
        [Serializable]
        public struct SubtitleCue
        {
            [Tooltip("Tempo em segundos a partir do início da cutscene em que a legenda aparece.")]
            [SerializeField]
            [Min(0f)]
            private float _startTime;

            [Tooltip("Duração em segundos que a legenda permanece visível.")]
            [SerializeField]
            [Min(0.1f)]
            private float _duration;

            [Tooltip("Nome do personagem exibido acima da legenda.")]
            [SerializeField]
            private string _speaker;

            [Tooltip("Texto da legenda exibido durante o intervalo de tempo configurado.")]
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

            /// <summary>Tempo de início da legenda em segundos.</summary>
            public readonly float StartTime => _startTime;

            /// <summary>Duração mínima garantida de 0.1s.</summary>
            public readonly float Duration => Mathf.Max(0.1f, _duration);

            /// <summary>Nome do personagem falante.</summary>
            public readonly string Speaker => _speaker;

            /// <summary>Texto da legenda.</summary>
            public readonly string Text => _text;

            /// <summary>Tempo de fim calculado (StartTime + Duration).</summary>
            public readonly float EndTime => StartTime + Duration;

            /// <summary>Retorna true se o tempo fornecido está dentro do intervalo da legenda.</summary>
            public readonly bool IsActive(float time) => time >= StartTime && time <= EndTime;
        }

        [Header("Timeline")]
        [Tooltip(
            "Director de Timeline usado para reprodução cinemática. Se nulo, usa fallback por shots."
        )]
        [SerializeField]
        private PlayableDirector _timelineDirector;

        [Header("Câmera")]
        [Tooltip(
            "Câmera dedicada da cutscene. Ativada durante a reprodução; a câmera do jogador é desativada."
        )]
        [SerializeField]
        private Camera _cutsceneCamera;

        [Header("Controles de Reprodução")]
        [Tooltip("Permite que o jogador pule a cutscene com Interagir, Atacar ou Pular.")]
        [SerializeField]
        private bool _allowSkip = true;

        [Header("Letterbox")]
        [Tooltip("Exibe barras pretas de letterbox durante a cutscene.")]
        [SerializeField]
        private bool _showLetterbox = true;

        [Tooltip("Altura normalizada das barras de letterbox (0.05–0.25).")]
        [SerializeField]
        [Range(0.05f, 0.25f)]
        private float _letterboxHeightNormalized = 0.12f;

        [Header("Fade")]
        [Tooltip("Se true, inicia com tela preta e faz fade-in ao começar.")]
        [SerializeField]
        private bool _fadeInFromBlack = true;

        [Tooltip("Duração do fade-in em segundos.")]
        [SerializeField]
        [Min(0f)]
        private float _fadeInDuration = 0.8f;

        [Tooltip("Se true, faz fade-out para preto ao terminar.")]
        [SerializeField]
        private bool _fadeOutToBlack;

        [Tooltip("Duração do fade-out em segundos.")]
        [SerializeField]
        [Min(0f)]
        private float _fadeOutDuration = 0.8f;

        [Header("Bloqueio do Jogador")]
        [Tooltip("Bloqueia o movimento do jogador durante a cutscene.")]
        [SerializeField]
        private bool _lockMovement = true;

        [Tooltip("Bloqueia a câmera do jogador durante a cutscene.")]
        [SerializeField]
        private bool _lockLook = true;

        [Tooltip("Bloqueia as interações do jogador durante a cutscene.")]
        [SerializeField]
        private bool _lockInteraction = true;

        [Header("Fallback por Shots")]
        [Tooltip("Duração da transição entre shots de câmera no fallback (sem Timeline).")]
        [SerializeField]
        [Min(0f)]
        private float _shotBlendDuration = 0.75f;

        [Tooltip("Duração mínima do fallback quando não há shots ou legendas definidas.")]
        [SerializeField]
        [Min(0f)]
        private float _fallbackDuration = 2f;

        [Tooltip("Lista de planos de câmera para cutscenes sem Timeline.")]
        [SerializeField]
        private CameraShot[] _cameraShots = Array.Empty<CameraShot>();

        [Tooltip("Entradas de legenda sincronizadas ao tempo de reprodução.")]
        [SerializeField]
        private SubtitleCue[] _subtitleCues = Array.Empty<SubtitleCue>();

        /// <summary>Director de Timeline associado. Pode ser nulo.</summary>
        public PlayableDirector TimelineDirector => _timelineDirector;

        /// <summary>Câmera da cutscene. Ativada durante a reprodução.</summary>
        public Camera CutsceneCamera => _cutsceneCamera;

        /// <summary>Se true, o jogador pode pular a cutscene.</summary>
        public bool AllowSkip => _allowSkip;

        /// <summary>Se true, exibe letterbox durante a cutscene.</summary>
        public bool ShowLetterbox => _showLetterbox;

        /// <summary>Altura normalizada do letterbox (0.05–0.25).</summary>
        public float LetterboxHeightNormalized => _letterboxHeightNormalized;

        /// <summary>Se true, inicia com fade-in a partir do preto.</summary>
        public bool FadeInFromBlack => _fadeInFromBlack;

        /// <summary>Duração do fade-in em segundos.</summary>
        public float FadeInDuration => Mathf.Max(0f, _fadeInDuration);

        /// <summary>Se true, encerra com fade-out para preto.</summary>
        public bool FadeOutToBlack => _fadeOutToBlack;

        /// <summary>Duração do fade-out em segundos.</summary>
        public float FadeOutDuration => Mathf.Max(0f, _fadeOutDuration);

        /// <summary>Bloqueia o movimento do jogador durante a reprodução.</summary>
        public bool LockMovement => _lockMovement;

        /// <summary>Bloqueia a câmera do jogador durante a reprodução.</summary>
        public bool LockLook => _lockLook;

        /// <summary>Bloqueia as interações do jogador durante a reprodução.</summary>
        public bool LockInteraction => _lockInteraction;

        /// <summary>Duração da transição entre shots no fallback.</summary>
        public float ShotBlendDuration => Mathf.Max(0f, _shotBlendDuration);

        /// <summary>Lista de shots de câmera para o fallback. Nunca retorna null.</summary>
        public CameraShot[] CameraShots => _cameraShots ?? Array.Empty<CameraShot>();

        /// <summary>Retorna true se há um PlayableDirector com asset de Timeline configurado.</summary>
        public bool UsesTimeline =>
            _timelineDirector != null && _timelineDirector.playableAsset != null;

        /// <summary>
        /// Configura a sequência via código para modo fallback (sem Timeline).
        /// Útil em testes ou bootstrap programático.
        /// </summary>
        public void ConfigureFallback(FallbackConfig config)
        {
            _timelineDirector = null;
            _cutsceneCamera = config.CutsceneCamera;
            _cameraShots = config.CameraShots;
            _subtitleCues = config.SubtitleCues;
            _fadeInFromBlack = config.FadeInFromBlack;
            _fadeInDuration = config.FadeInDuration;
            _fadeOutToBlack = config.FadeOutToBlack;
            _fadeOutDuration = config.FadeOutDuration;
            _allowSkip = config.AllowSkip;
            _shotBlendDuration = config.ShotBlendDuration;
            _fallbackDuration = config.FallbackDuration;
            _showLetterbox = config.ShowLetterbox;
            _letterboxHeightNormalized = config.LetterboxHeightNormalized;
            _lockMovement = config.LockMovement;
            _lockLook = config.LockLook;
            _lockInteraction = config.LockInteraction;
        }

        /// <summary>
        /// Calcula a duração total de reprodução. Usa a duração da Timeline se disponível;
        /// caso contrário, o maior entre: fallbackDuration, soma dos shots e última legenda.
        /// </summary>
        public float GetPlaybackDuration()
        {
            if (UsesTimeline)
                return Mathf.Max(0.01f, (float)_timelineDirector.duration);

            var latestSubtitleEnd = 0f;
            for (var i = 0; i < _subtitleCues.Length; i++)
                latestSubtitleEnd = Mathf.Max(latestSubtitleEnd, _subtitleCues[i].EndTime);

            var shotDuration = 0f;
            if (_cameraShots != null && _cameraShots.Length > 0)
            {
                for (var i = 0; i < _cameraShots.Length; i++)
                    shotDuration += _cameraShots[i].HoldDuration;

                shotDuration += Mathf.Max(0, _cameraShots.Length - 1) * ShotBlendDuration;
            }

            return Mathf.Max(0.1f, _fallbackDuration, latestSubtitleEnd, shotDuration);
        }

        /// <summary>
        /// Tenta retornar a legenda ativa no tempo fornecido.
        /// </summary>
        /// <returns>True se existe uma legenda ativa no tempo fornecido.</returns>
        public bool TryGetSubtitle(float time, out SubtitleCue subtitleCue)
        {
            for (var i = 0; i < _subtitleCues.Length; i++)
            {
                if (_subtitleCues[i].IsActive(time))
                {
                    subtitleCue = _subtitleCues[i];
                    return true;
                }
            }

            subtitleCue = default;
            return false;
        }
    }
}
