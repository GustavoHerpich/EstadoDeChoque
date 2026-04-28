using UnityEngine;
using UnityEngine.Rendering;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Visuals
{
    [AddComponentMenu("EstadoDeChoque/Visual Attachment Slot")]
    [DisallowMultipleComponent]
    public sealed class VisualAttachmentSlot : MonoBehaviour
    {
        [SerializeField]
        private string _instanceName = "Visual";

        [SerializeField]
        private VisualAssetReference _visual = new();

        [SerializeField]
        private bool _renderShadowsOnly;

        [SerializeField]
        private Transform _fallbackRenderRoot;

        [SerializeField]
        private bool _applyOnAwake;

        private GameObject _visualInstance;

        private void Reset()
        {
            _visual ??= new VisualAssetReference();
        }

        private void OnValidate()
        {
            _visual ??= new VisualAssetReference();
        }

        private void Awake()
        {
            if (_applyOnAwake)
            {
                ApplyConfiguredVisual();
            }
        }

        public void Configure(
            VisualAssetReference visual,
            bool renderShadowsOnly,
            Transform fallbackRenderRoot = null,
            string instanceName = null
        )
        {
            _visual ??= new VisualAssetReference();
            _visual.CopyFrom(visual);
            _renderShadowsOnly = renderShadowsOnly;
            _fallbackRenderRoot = fallbackRenderRoot;

            if (!string.IsNullOrWhiteSpace(instanceName))
            {
                _instanceName = instanceName;
            }
        }

        /// <summary>
        /// Define se o visual deve ser renderizado apenas como sombra.
        /// </summary>
        public void SetRenderShadowOnly(bool renderShadowsOnly)
        {
            _renderShadowsOnly = renderShadowsOnly;
        }

        public GameObject ApplyConfiguredVisual()
        {
            if (_visual == null || !_visual.HasPrefab)
            {
                return null;
            }

            if (_visualInstance != null)
            {
                return _visualInstance;
            }

            _visualInstance = Instantiate(_visual.Prefab, transform, false);
            _visualInstance.name = _instanceName;
            _visualInstance.transform.SetLocalPositionAndRotation(
                _visual.LocalPosition,
                _visual.LocalRotation
            );
            _visualInstance.transform.localScale = _visual.LocalScale;

            SanitizeVisualInstance(_visualInstance, _renderShadowsOnly);
            HideFallbackRenderers(
                _fallbackRenderRoot != null ? _fallbackRenderRoot : transform,
                _visualInstance.transform
            );
            return _visualInstance;
        }

        public void ClearAttachedVisual()
        {
            if (_visualInstance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_visualInstance);
            }
            else
            {
                DestroyImmediate(_visualInstance);
            }

            _visualInstance = null;
        }

        public GameObject RebuildVisual()
        {
            ClearAttachedVisual();
            return ApplyConfiguredVisual();
        }

        private static void SanitizeVisualInstance(
            GameObject visualInstance,
            bool renderShadowsOnly
        )
        {
            DisableBehaviours<Camera>(visualInstance);
            DisableBehaviours<Light>(visualInstance);
            DisableBehaviours<AudioListener>(visualInstance);

            Collider[] colliders = visualInstance.GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                colliders[index].enabled = false;
            }

            Rigidbody[] rigidbodies = visualInstance.GetComponentsInChildren<Rigidbody>(true);
            for (var index = 0; index < rigidbodies.Length; index++)
            {
                rigidbodies[index].isKinematic = true;
                rigidbodies[index].detectCollisions = false;
            }

            Renderer[] renderers = visualInstance.GetComponentsInChildren<Renderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                renderers[index].shadowCastingMode = renderShadowsOnly
                    ? ShadowCastingMode.ShadowsOnly
                    : ShadowCastingMode.On;
            }
        }

        private static void DisableBehaviours<T>(GameObject visualInstance)
            where T : Behaviour
        {
            T[] components = visualInstance.GetComponentsInChildren<T>(true);
            for (var index = 0; index < components.Length; index++)
            {
                components[index].enabled = false;
            }
        }

        private static void HideFallbackRenderers(Transform fallbackRoot, Transform keepVisibleRoot)
        {
            if (fallbackRoot == null)
            {
                return;
            }

            Renderer[] renderers = fallbackRoot.GetComponentsInChildren<Renderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                if (
                    keepVisibleRoot != null
                    && renderers[index].transform.IsChildOf(keepVisibleRoot)
                )
                {
                    continue;
                }

                renderers[index].enabled = false;
            }
        }
    }
}
