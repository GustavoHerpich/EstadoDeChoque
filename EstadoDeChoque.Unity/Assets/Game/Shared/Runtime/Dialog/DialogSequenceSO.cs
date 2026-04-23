using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Dialog
{
    /// <summary>
    /// ScriptableObject que representa uma sequência de diálogo com título e páginas de texto.
    /// Use para externalizar diálogos hardcoded e facilitar localização e edição.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DialogSequence",
        menuName = "EstadoDeChoque/Dialog/Dialog Sequence"
    )]
    public sealed class DialogSequenceSO : ScriptableObject
    {
        [Tooltip("Título exibido no cabeçalho do modal de diálogo.")]
        [SerializeField]
        private string _title;

        [Tooltip("Cada entrada representa uma página do diálogo. O jogador avança manualmente.")]
        [SerializeField]
        [TextArea(2, 6)]
        private string[] _pages = System.Array.Empty<string>();

        /// <summary>Título do diálogo exibido no modal.</summary>
        public string Title => _title;

        /// <summary>Páginas do diálogo. Nunca retorna null.</summary>
        public string[] Pages => _pages ?? System.Array.Empty<string>();

        /// <summary>Retorna true se houver ao menos uma página de conteúdo.</summary>
        public bool HasContent => _pages != null && _pages.Length > 0;

        private void OnValidate()
        {
            _pages ??= System.Array.Empty<string>();
        }
    }
}
