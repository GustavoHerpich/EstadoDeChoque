using System;
using System.Collections;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Cutscenes;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Dialog;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Hospital
{
    /// <summary>
    /// Orquestra o fluxo narrativo da cena introdutória do hospital.
    /// Gerencia estados de interação, cutscenes e sequências de diálogo.
    /// </summary>
    public sealed class HospitalIntroFlowController : MonoBehaviour
    {
        private static readonly WaitForSeconds _waitForSeconds1_2 = new(1.2f);
        private static readonly WaitForSeconds _waitForSeconds0_25 = new(0.25f);

        private enum FlowState
        {
            None,
            IntroCutscene,
            WaitingForChart,
            WaitingForThomas,
            WaitingForBed,
            HeatCutscene,
            Completed,
        }

        [Header("Referências do Jogador e HUD")]
        [Tooltip("Compositor do jogador responsável por controle de input e câmera.")]
        [SerializeField]
        private PlayerComposer _player;

        [Tooltip("Presenter do HUD de gameplay para modais, objetivos e fade.")]
        [SerializeField]
        private GameplayHudPresenter _hud;

        [Header("Interactables")]
        [Tooltip("Ficha médica interagível. Disponibilizada após o despertar de Dante.")]
        [SerializeField]
        private MedicalChartInteractable _medicalChart;

        [Tooltip("NPC Thomas. Disponibilizado após leitura da ficha médica.")]
        [SerializeField]
        private HospitalNpcInteractable _thomas;

        [Tooltip("Maca de descanso. Disponibilizada após o diálogo com Thomas.")]
        [SerializeField]
        private BedRestInteractable _bed;

        [Header("Cutscenes")]
        [Tooltip("Player de cutscenes utilizado para reproduzir sequências cinemáticas.")]
        [SerializeField]
        private CutscenePlayer _cutscenePlayer;

        [Tooltip("Sequência de cutscene da intro (corredor do hospital).")]
        [SerializeField]
        private CutsceneSequence _introCutscene;

        [Tooltip("Sequência de cutscene da crise de calor de Dante.")]
        [SerializeField]
        private CutsceneSequence _heatCutscene;

        [Header("Diálogos")]
        [Tooltip("Diálogo exibido quando o jogador interage com Thomas.")]
        [SerializeField]
        private DialogSequenceSO _thomasDialog;

        [Tooltip(
            "Fallback textual para a cutscene do corredor quando a Timeline não está disponível."
        )]
        [SerializeField]
        private DialogSequenceSO _corridorCutsceneDialog;

        [Tooltip(
            "Fallback textual para a cutscene de calor intenso quando a Timeline não está disponível."
        )]
        [SerializeField]
        private DialogSequenceSO _heatCutsceneFallbackDialog;

        private FlowState _state;
        private string _activeModalTitle;
        private string[] _activeModalPages;
        private int _activeModalIndex;
        private Action _activeModalCompleted;
        private float _ignoreAdvanceUntil;

        /// <summary>
        /// Configura todas as dependências do controller via código.
        /// Útil para injeção em testes ou bootstrap de cena.
        /// </summary>
        public void Configure(
            PlayerComposer player,
            GameplayHudPresenter hud,
            MedicalChartInteractable medicalChart,
            HospitalNpcInteractable thomas,
            BedRestInteractable bed,
            CutscenePlayer cutscenePlayer,
            CutsceneSequence introCutscene,
            CutsceneSequence heatCutscene,
            DialogSequenceSO thomasDialog,
            DialogSequenceSO corridorCutsceneDialog,
            DialogSequenceSO heatCutsceneFallbackDialog
        )
        {
            _player = player;
            _hud = hud;
            _medicalChart = medicalChart;
            _thomas = thomas;
            _bed = bed;
            _cutscenePlayer = cutscenePlayer;
            _introCutscene = introCutscene;
            _heatCutscene = heatCutscene;
            _thomasDialog = thomasDialog;
            _corridorCutsceneDialog = corridorCutsceneDialog;
            _heatCutsceneFallbackDialog = heatCutsceneFallbackDialog;
        }

        private IEnumerator Start()
        {
            yield return null;

            if (_player == null || _hud == null)
                yield break;

            _medicalChart?.SetAvailability(false);
            _thomas?.SetAvailability(false);
            _bed?.SetAvailability(false);

            _player.SetControlState(false, false, false);
            _hud.HideModal();
            _hud.ShowObjective(string.Empty);
            _hud.SetFade(1f);

            yield return _waitForSeconds0_25;

            _state = FlowState.IntroCutscene;
            if (
                _cutscenePlayer != null
                && _introCutscene != null
                && _cutscenePlayer.TryPlay(_introCutscene, BeginWakeUpSequence)
            )
            {
                yield break;
            }

            if (_corridorCutsceneDialog != null && _corridorCutsceneDialog.HasContent)
            {
                OpenModalSequence(
                    _corridorCutsceneDialog.Title,
                    _corridorCutsceneDialog.Pages,
                    BeginWakeUpSequence
                );
            }
            else
            {
                BeginWakeUpSequence();
            }
        }

        private void Update()
        {
            if (_activeModalPages == null || _player == null)
                return;

            if (Time.unscaledTime < _ignoreAdvanceUntil)
                return;

            PlayerInputFrame input = _player.CurrentInputFrame;
            if (input.InteractPressed || input.AttackPressed || input.JumpPressed)
                AdvanceModal();
        }

        /// <summary>
        /// Chamado pelo sistema de interação quando o jogador interage com a ficha médica.
        /// </summary>
        public void HandleMedicalChartInteracted(MedicalChartInteractable medicalChart)
        {
            if (_state != FlowState.WaitingForChart || medicalChart == null)
                return;

            OpenModalSequence(
                medicalChart.DocumentTitle,
                new[] { medicalChart.DocumentBody },
                () =>
                {
                    _state = FlowState.WaitingForThomas;
                    _thomas?.SetAvailability(true);
                    _player.SetControlState(false, true, true);
                    _hud.ShowObjective("Fale com Thomas.");
                    _hud.ShowMessage("Thomas percebe que Dante acordou.", 2f);
                }
            );
        }

        /// <summary>
        /// Chamado pelo sistema de interação quando o jogador interage com um NPC.
        /// </summary>
        public void HandleNpcInteracted(HospitalNpcInteractable npc)
        {
            if (_state != FlowState.WaitingForThomas || npc == null)
                return;

            string title =
                (_thomasDialog != null && !string.IsNullOrWhiteSpace(_thomasDialog.Title))
                    ? _thomasDialog.Title
                    : npc.DisplayName;

            string[] pages =
                _thomasDialog != null && _thomasDialog.HasContent
                    ? _thomasDialog.Pages
                    : Array.Empty<string>();

            OpenModalSequence(
                title,
                pages,
                () =>
                {
                    _state = FlowState.WaitingForBed;
                    _bed?.SetAvailability(true);
                    _player.SetControlState(true, true, true);
                    _hud.ShowObjective("Volte para a maca.");
                    _hud.ShowMessage("Thomas recua, visivelmente desconfortavel.", 2.25f);
                }
            );
        }

        /// <summary>
        /// Chamado pelo sistema de interação quando o jogador interage com a maca.
        /// </summary>
        public void HandleBedInteracted(BedRestInteractable bedInteractable)
        {
            if (_state != FlowState.WaitingForBed || bedInteractable == null)
                return;

            StartCoroutine(RunHeatCutscene());
        }

        private IEnumerator RunHeatCutscene()
        {
            _state = FlowState.HeatCutscene;
            _player.SetControlState(false, false, false);
            _bed?.SetAvailability(false);
            _hud.ShowObjective(string.Empty);
            _hud.ShowMessage("Dante volta para a maca e tenta descansar.", 1.6f);

            if (
                _cutscenePlayer != null
                && _heatCutscene != null
                && _cutscenePlayer.TryPlay(_heatCutscene, OnHeatCutsceneCompleted)
            )
            {
                yield break;
            }

            yield return _waitForSeconds1_2;
            yield return _hud.Fade(0f, 1f, 1.6f);

            if (_heatCutsceneFallbackDialog != null && _heatCutsceneFallbackDialog.HasContent)
            {
                OpenModalSequence(
                    _heatCutsceneFallbackDialog.Title,
                    _heatCutsceneFallbackDialog.Pages,
                    OnHeatCutsceneCompleted
                );
            }
            else
            {
                OnHeatCutsceneCompleted();
            }
        }

        private void OnHeatCutsceneCompleted()
        {
            _state = FlowState.Completed;
            _hud.ShowObjective("Cena inicial do hospital concluida.");
            _hud.ShowMessage(
                "Sequencia inicial concluida. Voce pode continuar explorando o quarto.",
                3f
            );
            _player.SetControlState(true, true, true);
            StartCoroutine(_hud.Fade(1f, 0f, 1f));
        }

        private void OpenModalSequence(string title, string[] pages, Action onCompleted)
        {
            if (pages == null || pages.Length == 0)
            {
                onCompleted?.Invoke();
                return;
            }

            _activeModalTitle = title;
            _activeModalPages = pages;
            _activeModalIndex = 0;
            _activeModalCompleted = onCompleted;
            _ignoreAdvanceUntil = Time.unscaledTime + 0.15f;

            _player.SetControlState(false, false, false);
            _hud.ShowModal(_activeModalTitle, _activeModalPages[_activeModalIndex]);
        }

        private void AdvanceModal()
        {
            _activeModalIndex++;
            if (_activeModalPages != null && _activeModalIndex < _activeModalPages.Length)
            {
                _hud.ShowModal(_activeModalTitle, _activeModalPages[_activeModalIndex]);
                return;
            }

            _hud.HideModal();
            _activeModalPages = null;
            _activeModalIndex = 0;

            Action completed = _activeModalCompleted;
            _activeModalCompleted = null;
            completed?.Invoke();
        }

        private void BeginWakeUpSequence()
        {
            StartCoroutine(BeginWakeUpSequenceRoutine());
        }

        private IEnumerator BeginWakeUpSequenceRoutine()
        {
            _medicalChart?.SetAvailability(true);
            _thomas?.SetAvailability(false);
            _bed?.SetAvailability(false);

            _player.SetControlState(false, true, true);
            _hud.ShowObjective("Leia a ficha medica de Dante.");

            yield return _hud.Fade(1f, 0f, 1.4f);

            _hud.ShowMessage("Dante desperta confuso e desorientado na sala do hospital.", 2.5f);
            _state = FlowState.WaitingForChart;
        }
    }
}
