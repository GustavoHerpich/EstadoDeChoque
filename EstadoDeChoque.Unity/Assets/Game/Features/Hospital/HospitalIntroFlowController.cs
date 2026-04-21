using System;
using System.Collections;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Cutscenes;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Features.Hospital
{
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

        [SerializeField]
        private FirstPersonPlayerController _player;

        [SerializeField]
        private GameplayHudPresenter _hud;

        [SerializeField]
        private MedicalChartInteractable _medicalChart;

        [SerializeField]
        private HospitalNpcInteractable _thomas;

        [SerializeField]
        private BedRestInteractable _bed;

        [SerializeField]
        private CutscenePlayer _cutscenePlayer;

        [SerializeField]
        private CutsceneSequence _introCutscene;

        [SerializeField]
        private CutsceneSequence _heatCutscene;

        private readonly string[] _thomasDialogue =
        {
            "DANTE: Ugh... minha cabeca... Onde diabos eu estou?",
            "THOMAS: Calma, Dante. Voce esta no hospital. Parece que passou bem mal.",
            "DANTE: Mal? Sinto como se tivesse sido atropelado por um caminhao...",
            "THOMAS: Eu tambem nao entendi muito bem. So vim correndo quando recebi a ligacao.",
            "DANTE: Ja disseram quando eu posso sair? Voce sabe que eu odeio hospitais.",
            "THOMAS: Se voce descansar mais um pouco, devem te liberar logo. Tenta deitar de novo.",
        };

        private readonly string[] _corridorCutscene =
        {
            "Uma ambulancia corta a entrada do hospital. As portas duplas se abrem bruscamente e Dante e levado pelo corredor em uma maca.",
            "MEDICA: O que aconteceu?\nTHOMAS: Dante e meu amigo... eu nao sei muito bem. So recebi a ligacao e vim correndo.\nENFERMEIROS: Saiam da frente!",
        };

        private readonly string[] _heatCutsceneFallback =
        {
            "Dante acorda no meio da madrugada. O calor cresce de repente, o peito aperta e o som do hospital parece afundar sob a agua.",
            "A respiracao fica irregular. Antes que ele entenda o que esta acontecendo, a visao escurece. Algo dentro dele tentava despertar.",
        };

        private FlowState _state;
        private string _activeModalTitle;
        private string[] _activeModalPages;
        private int _activeModalIndex;
        private Action _activeModalCompleted;
        private float _ignoreAdvanceUntil;

        public void Configure(
            FirstPersonPlayerController player,
            GameplayHudPresenter hud,
            MedicalChartInteractable medicalChart,
            HospitalNpcInteractable thomas,
            BedRestInteractable bed,
            CutscenePlayer cutscenePlayer,
            CutsceneSequence introCutscene,
            CutsceneSequence heatCutscene
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
        }

        private IEnumerator Start()
        {
            yield return null;

            if (_player == null || _hud == null)
            {
                yield break;
            }

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

            OpenModalSequence("Corredor do Hospital", _corridorCutscene, BeginWakeUpSequence);
        }

        private void Update()
        {
            if (_activeModalPages == null || _player == null)
            {
                return;
            }

            if (Time.unscaledTime < _ignoreAdvanceUntil)
            {
                return;
            }

            PlayerInputFrame input = _player.CurrentInputFrame;
            if (input.InteractPressed || input.AttackPressed || input.JumpPressed)
            {
                AdvanceModal();
            }
        }

        public void HandleMedicalChartInteracted(MedicalChartInteractable medicalChart)
        {
            if (_state != FlowState.WaitingForChart || medicalChart == null)
            {
                return;
            }

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

        public void HandleNpcInteracted(HospitalNpcInteractable npc)
        {
            if (_state != FlowState.WaitingForThomas || npc == null)
            {
                return;
            }

            OpenModalSequence(
                npc.DisplayName,
                _thomasDialogue,
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

        public void HandleBedInteracted(BedRestInteractable bedInteractable)
        {
            if (_state != FlowState.WaitingForBed || bedInteractable == null)
            {
                return;
            }

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
            yield return Fade(0f, 1f, 1.6f);

            OpenModalSequence("Calor Intenso", _heatCutsceneFallback, OnHeatCutsceneCompleted);
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
            StartCoroutine(Fade(1f, 0f, 1f));
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

            yield return Fade(1f, 0f, 1.4f);

            _hud.ShowMessage("Dante desperta confuso e desorientado na sala do hospital.", 2.5f);
            _state = FlowState.WaitingForChart;
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = duration > 0.001f ? Mathf.Clamp01(elapsed / duration) : 1f;
                _hud.SetFade(Mathf.Lerp(fromAlpha, toAlpha, t));
                yield return null;
            }

            _hud.SetFade(toAlpha);
        }
    }
}
