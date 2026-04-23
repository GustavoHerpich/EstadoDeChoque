using EstadoDeChoque.Gameplay.Assets.Game.Features.Cutscenes;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Hospital;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Config;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Dialog;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Visuals;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EstadoDeChoque.Gameplay.Assets.Game.Bootstrap
{
    /// <summary>
    /// Bootstrap da cena introdutória do hospital. Instancia e conecta todos os sistemas
    /// em runtime sem dependência de prefabs pré-configurados na cena.
    /// Os diálogos do fluxo podem ser sobrescritos via <see cref="DialogSequenceSO"/> no Inspector;
    /// quando nulos, o controller usa o caminho de fallback interno.
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

        [Header("Componentes do Jogador")]
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

            Transform visualRoot = CreateChildRoot(
                playerRoot.transform,
                "PlayerVisualRoot",
                Vector3.zero,
                Quaternion.identity
            );
            AttachConfiguredVisual(
                visualRoot,
                "PlayerVisual",
                _playerVisual,
                _renderPlayerBodyAsShadowsOnly
            );

            playerRoot.AddComponent<PlayerInteractionSensor>();

            FirstPersonPlayerSettings settings =
                _playerSettings ?? FirstPersonPlayerSettings.CreateRuntimeDefaults();

            PlayerCompositionBuilder builder = new PlayerCompositionBuilder(playerRoot)
                .WithSettings(settings)
                .WithCamera(cameraToUse)
                .WithPivot(cameraPivot);

            if (_addPlayerAnimation)
                builder = builder.WithAnimation();

            builder.Build();

            PlayerComposer composer = playerRoot.AddComponent<PlayerComposer>();
            composer.SetHud(hud);
            return composer;
        }

        private void BuildHospitalIntroPrototype(PlayerComposer player, GameplayHudPresenter hud)
        {
            if (FindFirstObjectByType<HospitalIntroFlowController>() != null)
                return;

            var root = new GameObject("HospitalIntroPrototype");

            CreateCube(
                root.transform,
                "Floor",
                new Vector3(0f, -0.5f, 0f),
                new Vector3(12f, 1f, 10f),
                new Color(0.22f, 0.23f, 0.25f)
            );
            CreateCube(
                root.transform,
                "Ceiling",
                new Vector3(0f, 4.15f, 0f),
                new Vector3(12f, 0.4f, 10f),
                new Color(0.16f, 0.17f, 0.18f)
            );
            CreateCube(
                root.transform,
                "NorthWall",
                new Vector3(0f, 1.75f, 4.75f),
                new Vector3(12f, 4.5f, 0.5f),
                new Color(0.58f, 0.59f, 0.61f)
            );
            CreateCube(
                root.transform,
                "WestWall",
                new Vector3(-5.75f, 1.75f, 0f),
                new Vector3(0.5f, 4.5f, 10f),
                new Color(0.56f, 0.57f, 0.6f)
            );
            CreateCube(
                root.transform,
                "SouthWallLeft",
                new Vector3(-2.7f, 1.75f, -4.75f),
                new Vector3(6.1f, 4.5f, 0.5f),
                new Color(0.56f, 0.57f, 0.6f)
            );
            CreateCube(
                root.transform,
                "SouthWallRight",
                new Vector3(4.6f, 1.75f, -4.75f),
                new Vector3(2.8f, 4.5f, 0.5f),
                new Color(0.56f, 0.57f, 0.6f)
            );
            CreateCube(
                root.transform,
                "CorridorFloor",
                new Vector3(1.75f, -0.5f, -9.6f),
                new Vector3(4.5f, 1f, 9.2f),
                new Color(0.21f, 0.22f, 0.24f)
            );
            CreateCube(
                root.transform,
                "CorridorCeiling",
                new Vector3(1.75f, 4.15f, -9.6f),
                new Vector3(4.5f, 0.4f, 9.2f),
                new Color(0.16f, 0.17f, 0.18f)
            );
            CreateCube(
                root.transform,
                "CorridorWestWall",
                new Vector3(-0.25f, 1.75f, -9.6f),
                new Vector3(0.5f, 4.5f, 9.2f),
                new Color(0.54f, 0.55f, 0.58f)
            );
            CreateCube(
                root.transform,
                "CorridorEastWall",
                new Vector3(3.75f, 1.75f, -9.6f),
                new Vector3(0.5f, 4.5f, 9.2f),
                new Color(0.54f, 0.55f, 0.58f)
            );
            CreateCube(
                root.transform,
                "CorridorEndWall",
                new Vector3(1.75f, 1.75f, -14f),
                new Vector3(4.5f, 4.5f, 0.5f),
                new Color(0.52f, 0.53f, 0.56f)
            );
            CreateCube(
                root.transform,
                "DoorFrameLeft",
                new Vector3(0.12f, 1.75f, -4.82f),
                new Vector3(0.35f, 4.5f, 0.18f),
                new Color(0.29f, 0.3f, 0.33f)
            );
            CreateCube(
                root.transform,
                "DoorFrameRight",
                new Vector3(3.38f, 1.75f, -4.82f),
                new Vector3(0.35f, 4.5f, 0.18f),
                new Color(0.29f, 0.3f, 0.33f)
            );
            CreateCube(
                root.transform,
                "DoorFrameTop",
                new Vector3(1.75f, 3.82f, -4.82f),
                new Vector3(3.6f, 0.35f, 0.18f),
                new Color(0.29f, 0.3f, 0.33f)
            );
            CreateCube(
                root.transform,
                "DoorLeft",
                new Vector3(0.9f, 1.55f, -4.86f),
                new Vector3(1.25f, 3.2f, 0.08f),
                new Color(0.67f, 0.69f, 0.74f)
            );
            CreateCube(
                root.transform,
                "DoorRight",
                new Vector3(2.6f, 1.55f, -4.86f),
                new Vector3(1.25f, 3.2f, 0.08f),
                new Color(0.67f, 0.69f, 0.74f)
            );
            CreateCube(
                root.transform,
                "CorridorLightBoxA",
                new Vector3(1.75f, 3.88f, -7.2f),
                new Vector3(1.4f, 0.08f, 0.42f),
                new Color(0.8f, 0.85f, 0.92f)
            );
            CreateCube(
                root.transform,
                "CorridorLightBoxB",
                new Vector3(1.75f, 3.88f, -11.2f),
                new Vector3(1.4f, 0.08f, 0.42f),
                new Color(0.8f, 0.85f, 0.92f)
            );

            Transform gurneyRoot = CreateChildRoot(
                root.transform,
                "Gurney",
                new Vector3(1.75f, 0f, -10.4f),
                Quaternion.identity
            );
            CreateCube(
                gurneyRoot,
                "Frame",
                new Vector3(0f, 0.55f, 0f),
                new Vector3(0.85f, 0.12f, 2.2f),
                new Color(0.42f, 0.43f, 0.46f)
            );
            CreateCube(
                gurneyRoot,
                "Mattress",
                new Vector3(0f, 0.7f, 0f),
                new Vector3(0.76f, 0.16f, 2f),
                new Color(0.72f, 0.74f, 0.78f)
            );

            CreateCube(
                root.transform,
                "EastWallLower",
                new Vector3(5.75f, 1.1f, -1.65f),
                new Vector3(0.5f, 2.2f, 6.7f),
                new Color(0.57f, 0.58f, 0.61f)
            );
            CreateCube(
                root.transform,
                "EastWallUpper",
                new Vector3(5.75f, 3.45f, -1.65f),
                new Vector3(0.5f, 0.9f, 6.7f),
                new Color(0.57f, 0.58f, 0.61f)
            );
            CreateCube(
                root.transform,
                "EastWallNorth",
                new Vector3(5.75f, 1.75f, 3.8f),
                new Vector3(0.5f, 4.5f, 1.9f),
                new Color(0.57f, 0.58f, 0.61f)
            );
            CreateCube(
                root.transform,
                "Window",
                new Vector3(5.55f, 2f, 1.8f),
                new Vector3(0.08f, 1.55f, 2.2f),
                new Color(0.68f, 0.78f, 0.9f)
            );

            CreateCube(
                root.transform,
                "BathroomFloor",
                new Vector3(-3.7f, -0.5f, -3.2f),
                new Vector3(4f, 1f, 3f),
                new Color(0.2f, 0.21f, 0.23f)
            );
            CreateCube(
                root.transform,
                "BathroomWestWall",
                new Vector3(-5.75f, 1.75f, -3.2f),
                new Vector3(0.5f, 4.5f, 3f),
                new Color(0.5f, 0.51f, 0.54f)
            );
            CreateCube(
                root.transform,
                "BathroomSouthWall",
                new Vector3(-3.7f, 1.75f, -4.65f),
                new Vector3(4f, 4.5f, 0.5f),
                new Color(0.5f, 0.51f, 0.54f)
            );
            CreateCube(
                root.transform,
                "BathroomNorthWallLeft",
                new Vector3(-4.85f, 1.75f, -1.75f),
                new Vector3(1.7f, 4.5f, 0.5f),
                new Color(0.5f, 0.51f, 0.54f)
            );
            CreateCube(
                root.transform,
                "BathroomNorthWallRight",
                new Vector3(-2.55f, 1.75f, -1.75f),
                new Vector3(1.7f, 4.5f, 0.5f),
                new Color(0.5f, 0.51f, 0.54f)
            );
            CreateCube(
                root.transform,
                "BathroomMirror",
                new Vector3(-3.2f, 1.95f, -4.38f),
                new Vector3(1f, 0.9f, 0.05f),
                new Color(0.72f, 0.76f, 0.8f)
            );
            CreateCube(
                root.transform,
                "BathroomSink",
                new Vector3(-3.2f, 0.72f, -4.1f),
                new Vector3(0.9f, 0.18f, 0.45f),
                new Color(0.82f, 0.82f, 0.84f)
            );

            GameObject bedRoot = new("HospitalBed");
            bedRoot.transform.SetParent(root.transform, false);
            bedRoot.transform.localPosition = new Vector3(3.5f, 0f, 1.55f);
            CreateCube(
                bedRoot.transform,
                "Frame",
                Vector3.zero,
                new Vector3(1.3f, 0.35f, 2.55f),
                new Color(0.32f, 0.33f, 0.36f)
            );
            CreateCube(
                bedRoot.transform,
                "Mattress",
                new Vector3(0f, 0.28f, 0f),
                new Vector3(1.12f, 0.22f, 2.28f),
                new Color(0.74f, 0.75f, 0.78f)
            );
            CreateCube(
                bedRoot.transform,
                "Pillow",
                new Vector3(0f, 0.42f, 0.82f),
                new Vector3(0.72f, 0.12f, 0.34f),
                new Color(0.85f, 0.86f, 0.88f)
            );
            Transform bedVisualRoot = CreateChildRoot(
                bedRoot.transform,
                "VisualRoot",
                Vector3.zero,
                Quaternion.identity
            );
            AttachConfiguredVisual(
                bedVisualRoot,
                "BedVisual",
                _hospitalBedVisual,
                false,
                bedRoot.transform
            );

            Transform sideTableRoot = CreateChildRoot(
                root.transform,
                "SideTable",
                new Vector3(2.15f, 0.6f, 2f),
                Quaternion.identity
            );
            GameObject sideTableFallback = CreateCube(
                sideTableRoot,
                "Fallback",
                Vector3.zero,
                new Vector3(0.75f, 0.72f, 0.5f),
                new Color(0.36f, 0.3f, 0.25f)
            );
            Transform sideTableVisualRoot = CreateChildRoot(
                sideTableRoot,
                "VisualRoot",
                Vector3.zero,
                Quaternion.identity
            );
            AttachConfiguredVisual(
                sideTableVisualRoot,
                "SideTableVisual",
                _sideTableVisual,
                false,
                sideTableFallback.transform
            );

            Transform chartStandRoot = CreateChildRoot(
                sideTableRoot,
                "MedicalChartStand",
                new Vector3(0f, 0.38f, -0.02f),
                Quaternion.Euler(0f, -26f, 0f)
            );
            GameObject chartSupport = CreateCube(
                chartStandRoot,
                "Support",
                new Vector3(0f, 0.14f, 0.06f),
                new Vector3(0.12f, 0.28f, 0.12f),
                new Color(0.42f, 0.43f, 0.46f)
            );
            GameObject chartBackdrop = CreateCube(
                chartStandRoot,
                "Backdrop",
                new Vector3(0f, 0.34f, -0.03f),
                new Vector3(0.46f, 0.03f, 0.34f),
                new Color(0.72f, 0.73f, 0.76f)
            );
            GameObject chartObject = CreateCube(
                chartStandRoot,
                "MedicalChart",
                new Vector3(0f, 0.43f, -0.02f),
                new Vector3(0.46f, 0.04f, 0.32f),
                new Color(0.97f, 0.94f, 0.82f)
            );
            chartObject.transform.SetLocalPositionAndRotation(
                new Vector3(0f, 0.43f, -0.02f),
                Quaternion.Euler(68f, 0f, 0f)
            );
            CreateCube(
                chartStandRoot,
                "Clip",
                new Vector3(0f, 0.58f, -0.08f),
                new Vector3(0.19f, 0.04f, 0.05f),
                new Color(0.28f, 0.29f, 0.31f)
            );
            SetColliderEnabled(chartSupport, false);
            SetColliderEnabled(chartBackdrop, false);
            SetColliderEnabled(chartObject, false);

            GameObject chartInteractionZone = new("ChartInteractionZone");
            chartInteractionZone.transform.SetParent(chartStandRoot, false);
            chartInteractionZone.transform.localPosition = new Vector3(0f, 0.42f, -0.02f);
            BoxCollider chartCollider = chartInteractionZone.AddComponent<BoxCollider>();
            chartCollider.isTrigger = true;
            chartCollider.size = new Vector3(0.62f, 0.62f, 0.46f);

            Transform chartVisualRoot = CreateChildRoot(
                chartObject.transform,
                "VisualRoot",
                Vector3.zero,
                Quaternion.identity
            );
            AttachConfiguredVisual(
                chartVisualRoot,
                "ChartVisual",
                _medicalChartVisual,
                false,
                chartObject.transform
            );
            MedicalChartInteractable medicalChart =
                chartInteractionZone.AddComponent<MedicalChartInteractable>();

            Transform chairRoot = CreateChildRoot(
                root.transform,
                "ThomasChair",
                new Vector3(1.45f, 0f, 0.7f),
                Quaternion.Euler(0f, 64f, 0f)
            );
            CreateCube(
                chairRoot,
                "ChairSeat",
                new Vector3(0f, 0.45f, 0f),
                new Vector3(0.72f, 0.12f, 0.72f),
                new Color(0.32f, 0.25f, 0.21f)
            );
            CreateCube(
                chairRoot,
                "ChairBack",
                new Vector3(-0.31f, 1f, 0f),
                new Vector3(0.72f, 1f, 0.12f),
                new Color(0.32f, 0.25f, 0.21f)
            );
            Transform chairVisualRoot = CreateChildRoot(
                chairRoot,
                "VisualRoot",
                Vector3.zero,
                Quaternion.identity
            );
            AttachConfiguredVisual(chairVisualRoot, "ChairVisual", _chairVisual, false, chairRoot);

            GameObject thomasRoot = new("Thomas");
            thomasRoot.transform.SetParent(root.transform, false);
            thomasRoot.transform.SetLocalPositionAndRotation(
                new Vector3(1.45f, 0f, 0.7f),
                Quaternion.Euler(0f, 64f, 0f)
            );
            CreateCapsule(
                thomasRoot.transform,
                "FallbackBody",
                new Vector3(0f, 0.98f, 0f),
                new Vector3(0.68f, 1f, 0.68f),
                new Color(0.23f, 0.26f, 0.31f)
            );
            Transform thomasVisualRoot = CreateChildRoot(
                thomasRoot.transform,
                "VisualRoot",
                Vector3.zero,
                Quaternion.identity
            );
            AttachConfiguredVisual(
                thomasVisualRoot,
                "ThomasVisual",
                _thomasVisual,
                false,
                thomasRoot.transform
            );
            HospitalNpcInteractable thomasInteractable =
                thomasRoot.AddComponent<HospitalNpcInteractable>();

            GameObject bedInteraction = new("BedInteraction");
            bedInteraction.transform.SetParent(bedRoot.transform, false);
            bedInteraction.transform.localPosition = new Vector3(0f, 0.65f, -0.2f);
            BoxCollider bedCollider = bedInteraction.AddComponent<BoxCollider>();
            bedCollider.size = new Vector3(1.12f, 0.5f, 2.05f);
            BedRestInteractable bedInteractable =
                bedInteraction.AddComponent<BedRestInteractable>();

            CreateHospitalCutsceneSetup(
                root.transform,
                player,
                hud,
                out CutscenePlayer cutscenePlayer,
                out CutsceneSequence introCutscene,
                out CutsceneSequence heatCutscene
            );

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
                // DialogSequenceSOs são opcionais: nulos significam que o controller
                // usará o caminho de fallback interno (sem modal de texto).
                // Para customizar os diálogos, atribua os SOs no Inspector.
                _thomasDialog,
                _corridorCutsceneDialog,
                _heatCutsceneFallbackDialog
            );

            medicalChart.Configure(
                flow,
                "Ler ficha medica de Dante",
                "Ficha medica de Dante",
                "Paciente: Dante\nStatus: Observacao pos-incidente\nNotas: Trauma na cabeca, desorientacao, resposta de estresse elevada e padrao de sono instavel.\nObservacao: O paciente relata lapsos de memoria e desconforto ao ser questionado sobre o ocorrido."
            );
            thomasInteractable.Configure(flow, "Thomas", "Falar com Thomas");
            bedInteractable.Configure(flow, "Deitar novamente");
        }

        private static void CreateHospitalCutsceneSetup(
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
                EditorUtility.SetDirty(this);
#endif
        }

        private GameObject AttachConfiguredVisual(
            Transform visualRoot,
            string instanceName,
            VisualAssetReference visualReference,
            bool renderShadowOnly,
            Transform fallbackRenderRoot = null
        )
        {
            if (visualRoot == null || visualReference == null || !visualReference.HasPrefab)
                return null;

            if (!visualRoot.TryGetComponent(out VisualAttachmentSlot attachmentSlot))
                attachmentSlot = visualRoot.gameObject.AddComponent<VisualAttachmentSlot>();

            attachmentSlot.Configure(
                visualReference,
                renderShadowOnly,
                fallbackRenderRoot != null ? fallbackRenderRoot : visualRoot,
                instanceName
            );
            return attachmentSlot.ApplyConfiguredVisual();
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

        private static GameObject CreateCapsule(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color
        )
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name;
            capsule.transform.SetParent(parent, false);
            capsule.transform.localPosition = position;
            capsule.transform.localScale = scale;

            if (capsule.TryGetComponent(out Renderer renderer))
                renderer.material.color = color;

            return capsule;
        }

        private static GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color
        )
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;

            if (cube.TryGetComponent(out Renderer renderer))
                renderer.material.color = color;

            return cube;
        }

        private static void SetColliderEnabled(GameObject target, bool enabled)
        {
            if (target == null || !target.TryGetComponent(out Collider collider))
                return;

            collider.enabled = enabled;
        }
    }
}
