using System;
using UnityEngine;

namespace EstadoDeChoque.Gameplay.Assets.Game.Shared.Runtime.Visuals
{
    [Serializable]
    public sealed class VisualAssetReference
    {
        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private Vector3 _localPosition = Vector3.zero;

        [SerializeField]
        private Vector3 _localEulerAngles = Vector3.zero;

        [SerializeField]
        private Vector3 _localScale = Vector3.one;

        public GameObject Prefab => _prefab;

        public Vector3 LocalPosition => _localPosition;

        public Quaternion LocalRotation => Quaternion.Euler(_localEulerAngles);

        public Vector3 LocalScale => _localScale == Vector3.zero ? Vector3.one : _localScale;

        public bool HasPrefab => _prefab != null;

        public void CopyFrom(VisualAssetReference other)
        {
            if (other == null)
            {
                return;
            }

            _prefab = other._prefab;
            _localPosition = other._localPosition;
            _localEulerAngles = other._localEulerAngles;
            _localScale = other._localScale;
        }

        public bool TryAutoAssign(GameObject prefab)
        {
            if (_prefab != null || prefab == null)
            {
                return false;
            }

            _prefab = prefab;
            return true;
        }
    }
}
