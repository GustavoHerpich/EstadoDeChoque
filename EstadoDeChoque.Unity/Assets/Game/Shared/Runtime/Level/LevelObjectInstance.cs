using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Level
{
    /// <summary>
    /// Instância de um objeto a ser spawnado em um nível.
    /// Define transform, overrides de material/collider e configuração adicional.
    /// </summary>
    [System.Serializable]
    public struct LevelObjectInstance
    {
        [Tooltip("Prefab do objeto a ser instanciado.")]
        public GameObject Prefab;

        [Tooltip("Posição local relativa ao parent do layout (ou ao mundo se parent for null).")]
        public Vector3 LocalPosition;

        [Tooltip("Rotação local em Euler angles (graus).")]
        public Vector3 LocalEulerAngles;

        [Tooltip("Escala local do objeto.")]
        public Vector3 LocalScale;

        [Tooltip("Sobrescreve a cor padrão do material (se o renderer suportar tint).")]
        public Color? OverrideColor;

        [Tooltip("Sobrescreve o estado do collider (true = habilitado, false = desabilitado).")]
        public bool? ColliderEnabled;

        [Tooltip("Sobrescreve o flag renderShadowOnly no VisualAttachmentSlot, se presente.")]
        public bool? RenderShadowOnly;

        [Tooltip("Referência opcional a DialogSequenceSO para interações que exibem texto.")]
        public Object DialogAsset;

        /// <summary>
        /// Retorna a rotação como Quaternion.
        /// </summary>
        public readonly Quaternion LocalRotation => Quaternion.Euler(LocalEulerAngles);
    }
}
