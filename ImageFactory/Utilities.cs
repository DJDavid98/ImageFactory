using AssetBundleLoadingTools.Utilities;
using BeatSaberMarkupLanguage.Animations;
using ImageFactory.Models;
using SiraUtil.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ImageFactory
{
    internal static class Utilities
    {
        public static async Task<ProcessedAnimation> ProcessAnimation(AnimationFormat format, byte[] data)
        {
            AnimationData animationData;
            switch(format)
            {
                case AnimationFormat.GIF:
                    animationData = await AnimationLoader.ProcessGifAsync(data);
                    break;
                case AnimationFormat.APNG:
                    animationData = await AnimationLoader.ProcessApngAsync(data);
                    break;
                default:
                    throw new System.Exception($"Unknown animation format {format}");
            }

            return new ProcessedAnimation(animationData.atlas, animationData.uvs, animationData.delays, animationData.width, animationData.height);
        }

        private static Material _uiNoGlowRoundEdgeMaterial = null!;
        public static Material UINoGlowRoundEdge
        {
            get
            {
                if (_uiNoGlowRoundEdgeMaterial == null)
                {
                    _uiNoGlowRoundEdgeMaterial = GetResourceObjectByName<Material>("UINoGlowRoundEdge");
                }
                return _uiNoGlowRoundEdgeMaterial;
            }
        }

        private static Material _spritesMaterial = null!;
        public static Material SpritesMaterial
        {
            get
            {
                if (_spritesMaterial == null)
                {
                    var srcMaterialName = "Sprites-Default";
                    _spritesMaterial = new Material(GetResourceObjectByName<Material>(srcMaterialName));
                    _spritesMaterial.name = $"{srcMaterialName} (ImageFactory Clone)";
                }
                return _spritesMaterial;
            }

            set {
                _spritesMaterial = value;
            }
        }

        public static T GetResourceObjectByName<T>(string itemName) where T : UnityEngine.Object
        {
            var items = Resources.FindObjectsOfTypeAll<T>();
            var foundItem = items.FirstOrDefault(m => m.name == itemName);
            if (foundItem == null)
            {
                var joinString = "\n\t- ";
                var available = string.Join(joinString, new HashSet<string>(items.Select(m => m.name).ToArray()).OrderBy(name => name));
                throw new Exception($"Object {itemName} not found\nAvailable objects:{joinString}{available}");
            }
            return foundItem;
        }

        public static void InitializeCustomCellTableviewData(BeatSaberMarkupLanguage.Components.CustomCellListTableData _imageList, IList data, SiraLog logger)
        {
            try
            {
                _imageList.data = data;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to add images to list: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void FixShader(Material m, string debugSource, SiraLog logger)
        {
            var replacementInfo = ShaderRepair.FixShaderOnMaterial(m);
            if (!replacementInfo.AllShadersReplaced)
            {
                logger.Warn($"[{debugSource}] Missing shader replacement data:");
                foreach (var shaderName in replacementInfo.MissingShaderNames)
                {
                    logger.Warn($"\t- {shaderName}");
                }
            }
            else logger.Debug($"[{debugSource}] All shaders replaced.");
        }
    }
}
