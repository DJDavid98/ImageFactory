using BeatSaberMarkupLanguage.Animations;
using ImageFactory.Models;
using System.Linq;
using System.Text.RegularExpressions;
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

        private static Material _roundEdge = null!;
        public static Material UINoGlowRoundEdge
        {
            get
            {
                if (_roundEdge == null)
                {
                    _roundEdge = Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "UINoGlowRoundEdge");
                }
                return _roundEdge;
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
    }
}