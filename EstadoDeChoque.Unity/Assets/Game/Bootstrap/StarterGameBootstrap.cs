using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Features.UI;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Bootstrap
{
    public sealed class StarterGameBootstrap : MonoBehaviour
    {
        [SerializeField]
        private FirstPersonPlayerSettings _playerSettings;

        [SerializeField]
        private bool _buildStarterBlockout = true;

        [SerializeField]
        private bool _applyStarterAtmosphere = true;

        [SerializeField]
        private Vector3 _playerSpawnPosition = new(0f, 0f, -4f);

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            if (_applyStarterAtmosphere)
                ApplyStarterAtmosphere();

            GameplayHudPresenter hud = EnsureHud();
            Camera camera = EnsureCamera();
            EnsurePlayer(camera, hud);

            if (_buildStarterBlockout)
                EnsureStarterBlockout();
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
            {
                return existingHud;
            }

            var hudRoot = new GameObject("GameplayHud");
            return hudRoot.AddComponent<GameplayHudPresenter>();
        }

        private Camera EnsureCamera()
        {
            if (TryGetComponent(out Camera cameraToUse))
            {
                return cameraToUse;
            }

            cameraToUse = Camera.main;
            if (cameraToUse != null)
            {
                return cameraToUse;
            }

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
                .Build();

            PlayerComposer composer = playerRoot.AddComponent<PlayerComposer>();
            composer.SetHud(hud);
        }

        private void EnsureStarterBlockout()
        {
            var existingRoot = GameObject.Find("StarterBlockout");
            if (existingRoot != null)
            {
                return;
            }

            var blockoutRoot = new GameObject("StarterBlockout");

            CreateCube(
                blockoutRoot.transform,
                "Floor",
                new Vector3(0f, -0.5f, 0f),
                new Vector3(18f, 1f, 18f),
                new Color(0.19f, 0.2f, 0.22f)
            );
            CreateCube(
                blockoutRoot.transform,
                "Ceiling",
                new Vector3(0f, 4.25f, 0f),
                new Vector3(18f, 0.5f, 18f),
                new Color(0.12f, 0.12f, 0.14f)
            );

            CreateCube(
                blockoutRoot.transform,
                "NorthWall",
                new Vector3(0f, 1.75f, 8.75f),
                new Vector3(18f, 4.5f, 0.5f),
                new Color(0.23f, 0.23f, 0.25f)
            );
            CreateCube(
                blockoutRoot.transform,
                "SouthWallLeft",
                new Vector3(-5.5f, 1.75f, -8.75f),
                new Vector3(7f, 4.5f, 0.5f),
                new Color(0.22f, 0.22f, 0.24f)
            );
            CreateCube(
                blockoutRoot.transform,
                "SouthWallRight",
                new Vector3(5.5f, 1.75f, -8.75f),
                new Vector3(7f, 4.5f, 0.5f),
                new Color(0.22f, 0.22f, 0.24f)
            );
            CreateCube(
                blockoutRoot.transform,
                "WestWall",
                new Vector3(-8.75f, 1.75f, 0f),
                new Vector3(0.5f, 4.5f, 18f),
                new Color(0.24f, 0.24f, 0.27f)
            );
            CreateCube(
                blockoutRoot.transform,
                "EastWall",
                new Vector3(8.75f, 1.75f, 0f),
                new Vector3(0.5f, 4.5f, 18f),
                new Color(0.24f, 0.24f, 0.27f)
            );

            CreateCube(
                blockoutRoot.transform,
                "BackHallFloor",
                new Vector3(0f, -0.5f, -12f),
                new Vector3(5f, 1f, 6f),
                new Color(0.17f, 0.18f, 0.2f)
            );
            CreateCube(
                blockoutRoot.transform,
                "BackHallNorthWall",
                new Vector3(0f, 1.75f, -14.75f),
                new Vector3(5f, 4.5f, 0.5f),
                new Color(0.21f, 0.21f, 0.23f)
            );
            CreateCube(
                blockoutRoot.transform,
                "BackHallWestWall",
                new Vector3(-2.25f, 1.75f, -12f),
                new Vector3(0.5f, 4.5f, 6f),
                new Color(0.21f, 0.21f, 0.23f)
            );
            CreateCube(
                blockoutRoot.transform,
                "BackHallEastWall",
                new Vector3(2.25f, 1.75f, -12f),
                new Vector3(0.5f, 4.5f, 6f),
                new Color(0.21f, 0.21f, 0.23f)
            );

            CreateCube(
                blockoutRoot.transform,
                "Bed",
                new Vector3(-4.5f, 0.45f, 4f),
                new Vector3(1.2f, 0.35f, 2.3f),
                new Color(0.42f, 0.43f, 0.47f)
            );
            CreateCube(
                blockoutRoot.transform,
                "Table",
                new Vector3(3.6f, 0.65f, 3.4f),
                new Vector3(1.6f, 0.8f, 0.75f),
                new Color(0.32f, 0.27f, 0.23f)
            );
            CreateCube(
                blockoutRoot.transform,
                "Locker",
                new Vector3(-6.9f, 1.2f, -1.5f),
                new Vector3(0.9f, 2.4f, 1f),
                new Color(0.28f, 0.3f, 0.32f)
            );
            CreateCube(
                blockoutRoot.transform,
                "Crate",
                new Vector3(5.1f, 0.45f, -2.4f),
                new Vector3(1f, 0.9f, 1f),
                new Color(0.3f, 0.24f, 0.18f)
            );

            CreateNoteInteractable(blockoutRoot.transform);
            CreateHoldableItem(blockoutRoot.transform);
        }

        private static void CreateNoteInteractable(Transform parent)
        {
            GameObject clipboard = CreateCube(
                parent,
                "MedicalClipboard",
                new Vector3(3.55f, 1.12f, 3.4f),
                new Vector3(0.34f, 0.03f, 0.24f),
                new Color(0.77f, 0.76f, 0.72f)
            );
            clipboard.transform.rotation = Quaternion.Euler(76f, 0f, 0f);

            SimpleInteractionMessage note = clipboard.AddComponent<SimpleInteractionMessage>();
            note.Configure(
                "Ler ficha medica de Dante",
                "Placeholder inicial: Dante acordou desorientado. Use este fluxo de interacao para bilhetes, dialogos e gatilhos de historia."
            );
        }

        private static void CreateHoldableItem(Transform parent)
        {
            var lantern = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lantern.name = "LanternPickup";
            lantern.transform.SetParent(parent, false);
            lantern.transform.position = new Vector3(5.1f, 1.15f, -2.4f);
            lantern.transform.localScale = new Vector3(0.24f, 0.18f, 0.24f);

            if (lantern.TryGetComponent(out Renderer renderer))
            {
                renderer.material.color = new Color(0.65f, 0.58f, 0.36f);
            }

            HoldableItem item = lantern.AddComponent<HoldableItem>();
            item.Configure(
                "Lanterna",
                new Vector3(0.1f, -0.15f, 0.28f),
                new Vector3(15f, -70f, 0f)
            );
        }

        private static GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color
        )
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;

            if (cube.TryGetComponent(out Renderer renderer))
            {
                renderer.material.color = color;
            }

            return cube;
        }
    }
}
