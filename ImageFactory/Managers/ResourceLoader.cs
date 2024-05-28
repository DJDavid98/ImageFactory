using System;
using System.Linq;
using UnityEngine;

namespace ImageFactory.Managers
{
    internal class ResourceLoader
    {
        private Material? _cachedMaterial;

        public Material LoadSpriteMaterial()
        {
            if (_cachedMaterial != null)
                return _cachedMaterial;

            _cachedMaterial = Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "Sprites-Default");
            return _cachedMaterial;
        }
    }
}