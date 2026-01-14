using UnityEngine;

namespace CodeQLBootstrap
{
    /// <summary>
    /// Classe mínima apenas para permitir
    /// a execução do CodeQL em projetos Unity.
    /// </summary>
    public class CodeQLDummy : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("CodeQL enabled for Unity project.");
        }
    }
}
