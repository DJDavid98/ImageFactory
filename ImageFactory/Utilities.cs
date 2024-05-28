using BeatSaberMarkupLanguage.Animations;
using ImageFactory.Models;
using SiraUtil.Logging;
using System;
using System.Collections;
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
                    _uiNoGlowRoundEdgeMaterial = Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "UINoGlowRoundEdge");
                }
                return _uiNoGlowRoundEdgeMaterial;
            }
        }

        private static Material _spritesDefaultMaterial = null!;
        public static Material SpritesDefaultMaterial
        {
            get
            {
                if (_spritesDefaultMaterial == null)
                {
                    _spritesDefaultMaterial = Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "Sprites-Default");
                }
                return _spritesDefaultMaterial;
            }
        }

        private static Shader _shader = null!;
        public static Shader ImageShader
        {
            get
            {
                if (_shader == null)
                {
                    _shader = Resources.FindObjectsOfTypeAll<Shader>().First(s => s.name == "Custom/CustomParticles");
                }
                return _shader;
            }
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
    }
}
