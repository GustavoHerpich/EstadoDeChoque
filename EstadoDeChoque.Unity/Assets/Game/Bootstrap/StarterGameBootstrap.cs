using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Level;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Bootstrap
{
    /// <summary>
    /// Bootstrap inicial. Cria jogador e opcionalmente spawna blockout via LevelSpawner.
    /// Requer atribuição de _blockoutLayout no Inspector.
    /// </summary>
    public sealed class StarterGameBootstrap : MonoBehaviour
    {
        [SerializeField]
        private FirstPersonPlayerSettings _playerSettings;

        [SerializeField]
        private bool _applyStarterAtmosphere = true;

        [SerializeField]
        private Vector3 _playerSpawnPosition = new(0f, 0f, -4f);

        [Header("Layout Config")]
        [Tooltip("Layout do blockout inicial. Este asset deve conter todos os objetos da cena.")]
        [SerializeField]
        private LevelLayoutConfig _blockoutLayout;

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            if (_applyStarterAtmosphere)
                ApplyStarterAtmosphere();

            GameplayHudPresenter hud = EnsureHud();
            Camera camera = EnsureCamera();
            EnsurePlayer(camera, hud);

            if (_blockoutLayout != null)
            {
                var blockoutRoot = new GameObject("StarterBlockout");
                LevelSpawner spawner = blockoutRoot.AddComponent<LevelSpawner>();
                spawner.SpawnAll(_blockoutLayout);
            }
            else
            {
                Debug.LogWarning(
                    "[StarterGameBootstrap] Nenhum LevelLayoutConfig atribuído. Nenhum cenário será spawnado."
                );
            }
        }

        private void ApplyStarterAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.35f, 0.39f, 0.42f);
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.ambientIntensity = 0.75f;

            Light directionalLight = FindFirstObjectByType<Light>();
            if (directionalLight != null && directionalLight.type == LightType.Directional)
            {
                directionalLight.intensity = 0.8f;
                directionalLight.color = new Color(0.73f, 0.77f, 0.82f);
                directionalLight.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            }
        }

        private GameplayHudPresenter EnsureHud()
        {
            GameplayHudPresenter existingHud = FindFirstObjectByType<GameplayHudPresenter>();
            if (existingHud != null)
                return existingHud;

            var hudRoot = new GameObject("GameplayHud");
            return hudRoot.AddComponent<GameplayHudPresenter>();
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

        private void EnsurePlayer(Camera cameraToUse, GameplayHudPresenter hud)
        {
            if (FindFirstObjectByType<PlayerComposer>() != null)
                return;

            var playerRoot = new GameObject("Player");
            playerRoot.transform.position = _playerSpawnPosition;

            CharacterController cc = playerRoot.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.78f;
            cc.center = new Vector3(0f, 0.89f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;

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

            new PlayerCompositionBuilder(playerRoot)
                .WithSettings(_playerSettings ?? FirstPersonPlayerSettings.CreateRuntimeDefaults())
                .WithCamera(cameraToUse)
                .WithPivot(cameraPivot)
                .WithInput(playerRoot.AddComponent<FirstPersonInputSource>())
                .Build();

            PlayerComposer composer = playerRoot.AddComponent<PlayerComposer>();
            composer.SetHud(hud);
        }
    }
}
