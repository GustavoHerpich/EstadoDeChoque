using System.Collections.Generic;
using EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Visuals;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Level
{
    /// <summary>
    /// Componente que instancia objetos de um LevelLayoutConfig em runtime.
    /// Pode ser usado por bootstraps ou pela cena.
    /// </summary>
    public sealed class LevelSpawner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private LevelLayoutConfig _layoutConfig;

        [SerializeField]
        private bool _spawnOnAwake = true;

        [Header("Parenting")]
        [SerializeField]
        private Transform _rootParent;

        private readonly List<GameObject> _spawnedObjects = new();

        public IReadOnlyList<GameObject> SpawnedObjects => _spawnedObjects;

        private void Awake()
        {
            if (_spawnOnAwake && _layoutConfig != null)
            {
                SpawnAll();
            }
        }

        /// <summary>
        /// Spawna todos os objetos definidos no layout.
        /// </summary>
        public void SpawnAll(LevelLayoutConfig layout = null, Transform parentOverride = null)
        {
            ClearSpawned();

            LevelLayoutConfig config = layout ?? _layoutConfig;
            if (config == null)
            {
                Debug.LogError("[LevelSpawner] Layout config is null.");
                return;
            }

            Transform root = parentOverride != null ? parentOverride : _rootParent;
            if (root == null)
                root = transform;

            foreach (LevelLayoutConfig.LevelGroup group in config.Groups)
            {
                if (group.objects == null)
                    continue;

                foreach (LevelObjectInstance instance in group.objects)
                {
                    SpawnObject(instance, root);
                }
            }
        }

        /// <summary>
        /// Spawna um objeto específico.
        /// </summary>
        public GameObject SpawnObject(LevelObjectInstance instance, Transform parent = null)
        {
            Debug.Log($"[LevelSpawner] Spawning {instance.Prefab} at frame {Time.frameCount}");

            if (instance.Prefab == null)
            {
                Debug.LogError("[LevelSpawner] Prefab is null.");
                return null;
            }

            Transform root = parent != null ? parent : _rootParent;
            if (root == null)
                root = transform;

            GameObject go = Instantiate(instance.Prefab, root);
            Debug.Log(
                $"[LevelSpawner] Instantiated {go.name} parent={go.transform.parent?.name} localPos={go.transform.localPosition}"
            );

            ApplyInstanceTransform(go.transform, instance);
            Debug.Log(
                $"[LevelSpawner] After transform apply: pos={go.transform.localPosition} rot={go.transform.localEulerAngles} scale={go.transform.localScale}"
            );

            // Material override
            ApplyMaterialOverrides(go, instance);

            // Collider override
            if (instance.ColliderEnabled.HasValue)
            {
                Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
                foreach (Collider col in colliders)
                {
                    col.enabled = instance.ColliderEnabled.Value;
                }
            }

            // VisualAttachmentSlot processing
            VisualAttachmentSlot visualSlot = go.GetComponentInChildren<VisualAttachmentSlot>(true);
            if (visualSlot != null)
            {
                if (instance.RenderShadowOnly.HasValue)
                {
                    visualSlot.SetRenderShadowOnly(instance.RenderShadowOnly.Value);
                }
                visualSlot.ApplyConfiguredVisual();
            }

            // DialogAsset assignment
            if (instance.DialogAsset != null)
            {
                AssignDialogToInteractable(go, instance.DialogAsset);
            }

            _spawnedObjects.Add(go);
            return go;
        }

        private void ApplyInstanceTransform(Transform t, LevelObjectInstance instance)
        {
            t.localPosition = instance.LocalPosition;
            t.localRotation = instance.LocalRotation;
            t.localScale = instance.LocalScale;
        }

        private void ApplyMaterialOverrides(GameObject go, LevelObjectInstance instance)
        {
            if (instance.OverrideColor.HasValue)
            {
                Color color = instance.OverrideColor.Value;
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer rend in renderers)
                {
                    if (rend.sharedMaterial != null)
                    {
                        rend.material = new Material(rend.sharedMaterial) { color = color };
                    }
                }
            }
        }

        private void AssignDialogToInteractable(GameObject go, UnityEngine.Object dialogAsset)
        {
            // Future hook: assign DialogSequenceSO to IInteractable implementations that support it.
        }

        /// <summary>
        /// Remove todos os objetos spawnados desta configuração.
        /// </summary>
        public void ClearSpawned()
        {
            foreach (GameObject go in _spawnedObjects)
            {
                if (go != null)
                    Destroy(go);
            }
            _spawnedObjects.Clear();
        }

        private void OnDestroy()
        {
            ClearSpawned();
        }
    }
}
