using EstadoDeChoque.Gameplay.Assets.Game.Features.Interaction;
using EstadoDeChoque.Gameplay.Assets.Game.Features.Player;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.PlayerCore;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Bootstrap
{
    [AddComponentMenu("Bootstrap/Composed Player")]
    public sealed class ComposedPlayerBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private FirstPersonPlayerSettings _playerSettings;

        [SerializeField]
        private Vector3 _spawnPosition = new(0f, 0f, -4f);

        [Header("Composition")]
        [SerializeField]
        private bool _addMovement = true;

        [SerializeField]
        private bool _addLook = true;

        [SerializeField]
        private bool _addAnimation;

        [SerializeField]
        private bool _addSprite;

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            BuildPlayer();
        }

        private void BuildPlayer()
        {
            var playerRoot = new GameObject("Player");
            playerRoot.transform.position = _spawnPosition;

            FirstPersonPlayerSettings settings =
                _playerSettings ?? FirstPersonPlayerSettings.CreateRuntimeDefaults();

            Camera camera = ResolveCamera();
            Transform cameraPivot = null;

            // CharacterController apenas quando movimento está ativo
            // [RequireComponent] de PlayerMovement garante que CC exista — adicionamos aqui
            // explicitamente para controlar as dimensões antes de Build()
            if (_addMovement)
            {
                CharacterController cc = playerRoot.AddComponent<CharacterController>();
                cc.radius = 0.35f;
                cc.height = 1.78f;
                cc.center = new Vector3(0f, 0.89f, 0f);
                cc.stepOffset = 0.35f;
                cc.slopeLimit = 45f;
            }

            if (_addLook)
            {
                cameraPivot = new GameObject("CameraPivot").transform;
                cameraPivot.SetParent(playerRoot.transform, false);
                cameraPivot.localPosition = new Vector3(0f, 0.72f, 0f);

                camera.transform.SetParent(cameraPivot, false);
                camera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                camera.nearClipPlane = 0.05f;
                camera.fieldOfView = 72f;
            }

            // HandAnchor sempre presente — suporte a itens equipáveis
            Transform anchorParent = cameraPivot != null ? cameraPivot : playerRoot.transform;
            Transform handAnchor = new GameObject("HandAnchor").transform;
            handAnchor.SetParent(anchorParent, false);
            handAnchor.SetLocalPositionAndRotation(
                new Vector3(0.28f, -0.28f, 0.46f),
                Quaternion.Euler(8f, -6f, 0f)
            );
            handAnchor.gameObject.AddComponent<PlayerHoldItemAnchor>();
            playerRoot.AddComponent<PlayerInteractionSensor>();

            // Builder monta o grafo de composição —  Build() é obrigatório
            PlayerCompositionBuilder builder = new PlayerCompositionBuilder(playerRoot)
                .WithSettings(settings)
                .WithCamera(camera)
                .WithPivot(cameraPivot);

            if (!_addMovement)
                builder = builder.WithoutMovement();

            if (!_addLook)
                builder = builder.WithoutLook();

            if (_addAnimation)
                builder = builder.WithAnimation();

            if (_addSprite)
                builder = builder.WithSprite();

            builder.Build();

            // PlayerComposer por último — Awake encontra PlayerComposition já montada
            playerRoot.AddComponent<PlayerComposer>();
        }

        private static Camera ResolveCamera()
        {
            Camera existing = Camera.main;
            if (existing != null)
                return existing;

            var camObj = new GameObject("MainCamera") { tag = "MainCamera" };
            Camera camera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 72f;
            return camera;
        }
    }
}
