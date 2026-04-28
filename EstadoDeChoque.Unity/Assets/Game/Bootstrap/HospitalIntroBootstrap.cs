using EstadoDeChoque.Gameplay.Assets.Game.Features.Cutscenes;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Hospital;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Config;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Dialog;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Level;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Visuals;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EstadoDeChoque.Gameplay.Assets.Game.Bootstrap
{
    /// <summary>
    /// Bootstrap da cena introdutória do hospital.
    /// Usa LevelSpawner para instanciar todos os objetos de cenário a partir de um LevelLayoutConfig.
    /// Diálogos configuráveis via DialogSequenceSO assets.
    /// </summary>
    public sealed class HospitalIntroBootstrap : MonoBehaviour
    {
        private const string _defaultMainCharacterAssetPath =
            "Assets/Models/MainCharacter1.0.blend";

        [Header("Configurações do Jogador")]
        [Tooltip("Configurações de movimento e câmera do jogador em primeira pessoa.")]
        [SerializeField]
        private FirstPersonPlayerSettings _playerSettings;

        [Tooltip(
            "Aplica névoa, luz ambiente e iluminação direcional com tom hospitalar ao iniciar."
        )]
        [SerializeField]
        private bool _applyStarterAtmosphere = true;

        [Tooltip("Posição inicial de spawn do jogador na cena.")]
        [SerializeField]
        private Vector3 _playerSpawnPosition = new(2.95f, 0f, 1.52f);

        [Tooltip("Rotação inicial do jogador em graus no eixo Y (yaw).")]
        [SerializeField]
        private float _playerSpawnYaw = -126f;

        [Tooltip("Se true, o corpo do jogador é renderizado apenas como sombra (shadow only).")]
        [SerializeField]
        private bool _renderPlayerBodyAsShadowsOnly = true;

        [Header("Player Components")]
        [Tooltip(
            "Adiciona PlayerAnimation ao jogador. Requer AnimatorController configurado no prefab ou no PlayerVisual."
        )]
        [SerializeField]
        private bool _addPlayerAnimation = true;

        [Header("Visuais")]
        [Tooltip("Referência visual do modelo do jogador.")]
        [SerializeField]
        private VisualAssetReference _playerVisual = new();

        [Tooltip("Referência visual do personagem Thomas.")]
        [SerializeField]
        private VisualAssetReference _thomasVisual = new();

        [Tooltip("Referência visual da cama hospitalar.")]
        [SerializeField]
        private VisualAssetReference _hospitalBedVisual = new();

        [Tooltip("Referência visual da cadeira ao lado da cama.")]
        [SerializeField]
        private VisualAssetReference _chairVisual = new();

        [Tooltip("Referência visual da mesa de cabeceira.")]
        [SerializeField]
        private VisualAssetReference _sideTableVisual = new();

        [Tooltip("Referência visual da ficha médica.")]
        [SerializeField]
        private VisualAssetReference _medicalChartVisual = new();

        [Header("Layout da Cena")]
        [Tooltip(
            "Layout da sala do hospital. Contém todos os objetos arquitetônicos e mobiliário."
        )]
        [SerializeField]
        private LevelLayoutConfig _hospitalRoomLayout;

        [Header("Diálogos (opcional)")]
        [Tooltip(
            "Sequência de diálogo exibida ao interagir com Thomas. "
                + "Se nulo, o fluxo usa o fallback interno sem texto."
        )]
        [SerializeField]
        private DialogSequenceSO _thomasDialog;

        [Tooltip(
            "Diálogo de fallback exibido quando a Timeline da intro não está disponível. "
                + "Se nulo, a cutscene de intro pula direto para o despertar."
        )]
        [SerializeField]
        private DialogSequenceSO _corridorCutsceneDialog;

        [Tooltip(
            "Diálogo de fallback exibido quando a Timeline da crise de calor não está disponível. "
                + "Se nulo, o fluxo conclui sem modal de texto."
        )]
        [SerializeField]
        private DialogSequenceSO _heatCutsceneFallbackDialog;

        private void Reset()
        {
            EnsureVisualReferences();
        }

        private void OnValidate()
        {
            EnsureVisualReferences();
        }

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            EnsureVisualReferences();

            if (_applyStarterAtmosphere)
                ApplyHospitalAtmosphere();

            GameplayHudPresenter hud = EnsureHud();
            Camera cameraToUse = EnsureCamera();
            PlayerComposer player = EnsurePlayer(cameraToUse, hud);

            BuildHospitalIntroPrototype(player, hud);
        }

        private void ApplyHospitalAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.48f, 0.52f, 0.58f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.ambientIntensity = 0.65f;

            Light directionalLight = FindFirstObjectByType<Light>();
            if (directionalLight != null && directionalLight.type == LightType.Directional)
            {
                directionalLight.intensity = 0.55f;
                directionalLight.color = new Color(0.72f, 0.78f, 0.86f);
                directionalLight.transform.rotation = Quaternion.Euler(35f, -22f, 0f);
            }
        }

        private GameplayHudPresenter EnsureHud()
        {
            GameplayHudPresenter existingHud = FindFirstObjectByType<GameplayHudPresenter>();
            if (existingHud != null)
                return existingHud;

            return new GameObject("GameplayHud").AddComponent<GameplayHudPresenter>();
        }

        private Camera EnsureCamera()
        {
            if (TryGetComponent(out Camera cameraToUse))
                return cameraToUse;

            cameraToUse = Camera.main;
            if (cameraToUse != null)
                return cameraToUse;

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraToUse = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 1.6f, -10f);
            return cameraToUse;
        }

        private PlayerComposer EnsurePlayer(Camera cameraToUse, GameplayHudPresenter hud)
        {
            PlayerComposer existing = FindFirstObjectByType<PlayerComposer>();
            if (existing != null)
            {
                existing.SetHud(hud);
                if (_playerSettings != null)
                    existing.SetSettings(_playerSettings);
                return existing;
            }

            var playerRoot = new GameObject("Player");
            playerRoot.transform.SetPositionAndRotation(
                _playerSpawnPosition,
                Quaternion.Euler(0f, _playerSpawnYaw, 0f)
            );

            CharacterController cc = playerRoot.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.78f;
            cc.center = new Vector3(0f, 0.89f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0f;

            Transform cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(playerRoot.transform, false);
            cameraPivot.localPosition = new Vector3(0f, 0.72f, 0f);

            cameraToUse.transform.SetParent(cameraPivot, false);
            cameraToUse.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            cameraToUse.nearClipPlane = 0.05f;
            cameraToUse.fieldOfView = 72f;

            Transform handAnchor = new GameObject("HandAnchor").transform;
            handAnchor.SetParent(cameraPivot, false);
            handAnchor.SetLocalPositionAndRotation(
                new Vector3(0.28f, -0.28f, 0.46f),
                Quaternion.Euler(8f, -6f, 0f)
            );
            handAnchor.gameObject.AddComponent<PlayerHoldItemAnchor>();

            playerRoot.AddComponent<PlayerInteractionSensor>();

            FirstPersonPlayerSettings settings =
                _playerSettings ?? FirstPersonPlayerSettings.CreateRuntimeDefaults();

            new PlayerCompositionBuilder(playerRoot)
                .WithSettings(settings)
                .WithCamera(cameraToUse)
                .WithPivot(cameraPivot)
                .WithInput(playerRoot.AddComponent<FirstPersonInputSource>())
                .Build();

            PlayerComposer composer = playerRoot.AddComponent<PlayerComposer>();
            composer.SetHud(hud);
            return composer;
        }

        private void BuildHospitalIntroPrototype(PlayerComposer player, GameplayHudPresenter hud)
        {
            if (FindFirstObjectByType<HospitalIntroFlowController>() != null)
                return;

            var root = new GameObject("HospitalIntroPrototype");

            // Spawn entire room from layout config
            if (_hospitalRoomLayout != null)
            {
                LevelSpawner spawner = root.AddComponent<LevelSpawner>();
                spawner.SpawnAll(_hospitalRoomLayout);
            }
            else
            {
                Debug.LogError("[HospitalIntroBootstrap] HospitalRoomLayout is not assigned.");
                return;
            }

            // Cutscene setup
            CreateHospitalCutsceneSetup(
                root.transform,
                player,
                hud,
                out CutscenePlayer cutscenePlayer,
                out CutsceneSequence introCutscene,
                out CutsceneSequence heatCutscene
            );

            // Get references to interactables from spawned objects
            Transform rootTransform = root.transform;

            Transform chartZone = rootTransform.Find(
                "SideTable/MedicalChartStand/ChartInteractionZone"
            );
            MedicalChartInteractable medicalChart =
                chartZone != null ? chartZone.GetComponent<MedicalChartInteractable>() : null;

            Transform thomasTransform = rootTransform.Find("Thomas");
            HospitalNpcInteractable thomasInteractable =
                thomasTransform != null
                    ? thomasTransform.GetComponent<HospitalNpcInteractable>()
                    : null;

            Transform bedInteraction = rootTransform.Find("HospitalBed/BedInteraction");
            BedRestInteractable bedInteractable =
                bedInteraction != null ? bedInteraction.GetComponent<BedRestInteractable>() : null;

            HospitalIntroFlowController flow = root.AddComponent<HospitalIntroFlowController>();
            flow.Configure(
                player,
                hud,
                medicalChart,
                thomasInteractable,
                bedInteractable,
                cutscenePlayer,
                introCutscene,
                heatCutscene,
                _thomasDialog,
                _corridorCutsceneDialog,
                _heatCutsceneFallbackDialog
            );

            if (medicalChart != null)
            {
                medicalChart.Configure(
                    flow,
                    "Ler ficha medica de Dante",
                    "Ficha medica de Dante",
                    "Paciente: Dante\nStatus: Observacao pos-incidente\nNotas: Trauma na cabeca, desorientacao, resposta de estresse elevada e padrao de sono instavel.\nObservacao: O paciente relata lapsos de memoria e desconforto ao ser questionado sobre o ocorrido."
                );
            }

            if (thomasInteractable != null)
            {
                thomasInteractable.Configure(flow, "Thomas", "Falar com Thomas");
            }

            if (bedInteractable != null)
            {
                bedInteractable.Configure(flow, "Deitar novamente");
            }
        }

        private void CreateHospitalCutsceneSetup(
            Transform parent,
            PlayerComposer player,
            GameplayHudPresenter hud,
            out CutscenePlayer cutscenePlayer,
            out CutsceneSequence introCutscene,
            out CutsceneSequence heatCutscene
        )
        {
            Transform cutsceneRoot = CreateChildRoot(
                parent,
                "Cutscenes",
                Vector3.zero,
                Quaternion.identity
            );

            cutscenePlayer = cutsceneRoot.gameObject.AddComponent<CutscenePlayer>();
            cutscenePlayer.Configure(player, hud);
            Camera cutsceneCamera = CreateCutsceneCamera(cutsceneRoot, player.PlayerCamera);

            Transform introRoot = CreateChildRoot(
                cutsceneRoot,
                "IntroCutscene",
                Vector3.zero,
                Quaternion.identity
            );
            Transform introShotA = CreateChildRoot(
                introRoot,
                "ShotA",
                new Vector3(1.75f, 1.68f, -12.7f),
                Quaternion.Euler(4f, 0f, 0f)
            );
            Transform introShotB = CreateChildRoot(
                introRoot,
                "ShotB",
                new Vector3(1.75f, 1.55f, -7.5f),
                Quaternion.Euler(5f, 0f, 0f)
            );
            Transform introShotC = CreateChildRoot(
                introRoot,
                "ShotC",
                new Vector3(1.15f, 1.72f, 0.1f),
                Quaternion.Euler(11f, 60f, 0f)
            );

            introCutscene = introRoot.gameObject.AddComponent<CutsceneSequence>();
            introCutscene.ConfigureFallback(
                new FallbackConfig
                {
                    CutsceneCamera = cutsceneCamera,
                    CameraShots = new[]
                    {
                        new CutsceneSequence.CameraShot(introShotA, 1.6f),
                        new CutsceneSequence.CameraShot(introShotB, 1.5f),
                        new CutsceneSequence.CameraShot(introShotC, 1.8f),
                    },
                    SubtitleCues = new[]
                    {
                        new CutsceneSequence.SubtitleCue(
                            0.15f,
                            2.15f,
                            string.Empty,
                            "Uma ambulancia chega..."
                        ),
                        new CutsceneSequence.SubtitleCue(2.65f, 1.1f, "MEDICA", "O que aconteceu?"),
                        new CutsceneSequence.SubtitleCue(
                            3.85f,
                            1.7f,
                            "THOMAS",
                            "Dante e meu amigo..."
                        ),
                        new CutsceneSequence.SubtitleCue(
                            5.7f,
                            1f,
                            "ENFERMEIROS",
                            "Saiam da frente!"
                        ),
                    },
                    FadeInFromBlack = true,
                    FadeInDuration = 0.8f,
                    FadeOutToBlack = true,
                    FadeOutDuration = 0.8f,
                    FallbackDuration = 6.6f,
                }
            );

            Transform heatRoot = CreateChildRoot(
                cutsceneRoot,
                "HeatCutscene",
                Vector3.zero,
                Quaternion.identity
            );
            Transform heatShotA = CreateChildRoot(
                heatRoot,
                "ShotA",
                new Vector3(4.55f, 1.55f, 3.05f),
                Quaternion.Euler(8f, -145f, 0f)
            );
            Transform heatShotB = CreateChildRoot(
                heatRoot,
                "ShotB",
                new Vector3(2.05f, 1.42f, 1.38f),
                Quaternion.Euler(9f, 92f, 0f)
            );
            Transform heatShotC = CreateChildRoot(
                heatRoot,
                "ShotC",
                new Vector3(-3.15f, 1.7f, -3.7f),
                Quaternion.Euler(4f, 34f, 0f)
            );

            heatCutscene = heatRoot.gameObject.AddComponent<CutsceneSequence>();
            heatCutscene.ConfigureFallback(
                new FallbackConfig
                {
                    CutsceneCamera = cutsceneCamera,
                    CameraShots = new[]
                    {
                        new CutsceneSequence.CameraShot(heatShotA, 1.4f),
                        new CutsceneSequence.CameraShot(heatShotB, 1.4f),
                        new CutsceneSequence.CameraShot(heatShotC, 1.6f),
                    },
                    SubtitleCues = new[]
                    {
                        new CutsceneSequence.SubtitleCue(
                            0.2f,
                            2f,
                            string.Empty,
                            "Dante acorda no meio da madrugada. O quarto parece mais quente e o ar fica pesado."
                        ),
                        new CutsceneSequence.SubtitleCue(
                            2.5f,
                            2.1f,
                            string.Empty,
                            "O peito aperta, a respiracao falha e o hospital inteiro soa distante, como se estivesse submerso."
                        ),
                        new CutsceneSequence.SubtitleCue(
                            4.95f,
                            1.7f,
                            string.Empty,
                            "Algo dentro dele tentava despertar."
                        ),
                    },
                    FadeInFromBlack = false,
                    FadeInDuration = 0f,
                    FadeOutToBlack = true,
                    FadeOutDuration = 1.1f,
                    AllowSkip = true,
                    ShotBlendDuration = 0.7f,
                    FallbackDuration = 6.8f,
                    ShowLetterbox = true,
                    LetterboxHeightNormalized = 0.12f,
                    LockMovement = true,
                    LockLook = true,
                    LockInteraction = true,
                }
            );
        }

        private void EnsureVisualReferences()
        {
            _playerVisual ??= new VisualAssetReference();
            _thomasVisual ??= new VisualAssetReference();
            _hospitalBedVisual ??= new VisualAssetReference();
            _chairVisual ??= new VisualAssetReference();
            _sideTableVisual ??= new VisualAssetReference();
            _medicalChartVisual ??= new VisualAssetReference();

#if UNITY_EDITOR
            GameObject defaultCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(
                _defaultMainCharacterAssetPath
            );
            var changed =
                _playerVisual.TryAutoAssign(defaultCharacter)
                | _thomasVisual.TryAutoAssign(defaultCharacter);

            if (changed && !Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }

        private static Transform CreateChildRoot(
            Transform parent,
            string name,
            Vector3 localPosition,
            Quaternion localRotation
        )
        {
            Transform childRoot = new GameObject(name).transform;
            childRoot.SetParent(parent, false);
            childRoot.SetLocalPositionAndRotation(localPosition, localRotation);
            return childRoot;
        }

        private static Camera CreateCutsceneCamera(Transform parent, Camera referenceCamera)
        {
            GameObject cutsceneCameraObject = new("CutsceneCamera");
            cutsceneCameraObject.transform.SetParent(parent, false);

            Camera cutsceneCamera = cutsceneCameraObject.AddComponent<Camera>();
            cutsceneCamera.enabled = false;

            if (referenceCamera != null)
            {
                cutsceneCamera.fieldOfView = referenceCamera.fieldOfView;
                cutsceneCamera.nearClipPlane = referenceCamera.nearClipPlane;
                cutsceneCamera.farClipPlane = referenceCamera.farClipPlane;
                cutsceneCamera.clearFlags = referenceCamera.clearFlags;
                cutsceneCamera.backgroundColor = referenceCamera.backgroundColor;
                cutsceneCamera.cullingMask = referenceCamera.cullingMask;
            }

            return cutsceneCamera;
        }
    }
}
