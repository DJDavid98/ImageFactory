using BeatSaberMarkupLanguage.Animations;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ImageFactory.Models
{
    // Borred mostly from BSML
    public class RendererAnimationControllerData : AnimationControllerData
    {
        private readonly bool _isDelayConsistent = true;

        public RendererAnimationControllerData(Texture2D tex, Rect[] uvs, float[] delays) : base(tex, uvs, delays)
        {
            float firstDelay = -1;
            for (int i = 0; i < uvs.Length; i++)
            {
                if (i == 0)
                    firstDelay = delays[i];

                if (delays[i] != firstDelay)
                    _isDelayConsistent = false;
            }
        }

        public new void CheckFrame(DateTime now)
        {
            double differenceMs = (now - LastSwitch).TotalMilliseconds;
            if (differenceMs < Delays[UvIndex])
                return;

            if (_isDelayConsistent && Delays[UvIndex] <= 10 && differenceMs < 100)
            {
                // Bump animations with consistently 10ms or lower frame timings to 100ms
                return;
            }

            LastSwitch = now;
            do
            {
                UvIndex++;
                if (UvIndex >= this.uvs.Length)
                    UvIndex = 0;
            }
            while (!_isDelayConsistent && Delays[UvIndex] == 0);

            if (ActiveImages.Count != 0)
            {
                foreach (Image image in ActiveImages)
                {
                    if (image != null && image.isActiveAndEnabled)
                        image.sprite = Sprites[UvIndex];
                }
            }
        }
    }
}