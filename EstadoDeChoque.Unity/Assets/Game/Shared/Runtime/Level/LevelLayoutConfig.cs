using System.Collections.Generic;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Level
{
    /// <summary>
    /// Layout de nível: lista de objetos a serem spawnados.
    /// Pode conter múltiplos grupos para organização (arquitetura, props, personagens).
    /// </summary>
    [CreateAssetMenu(
        menuName = "EstadoDeChoque/Level/Layout Config",
        fileName = "LevelLayoutConfig"
    )]
    public sealed class LevelLayoutConfig : ScriptableObject
    {
        [System.Serializable]
        public struct LevelGroup
        {
            public string groupName;
            public List<LevelObjectInstance> objects;
        }

        [Header("Groups")]
        [Tooltip(
            "Agrupa objetos para organização do layout. Cada grupo pode ser spawnado separadamente se desejado."
        )]
        [SerializeField]
        private List<LevelGroup> _groups = new();

        public IReadOnlyList<LevelGroup> Groups => _groups;

        /// <summary>
        /// Retorna todos os objetos de todos os grupos como uma lista plana.
        /// </summary>
        public List<LevelObjectInstance> GetAllObjects()
        {
            var all = new List<LevelObjectInstance>();
            foreach (LevelGroup group in _groups)
            {
                if (group.objects != null)
                    all.AddRange(group.objects);
            }
            return all;
        }
    }
}
