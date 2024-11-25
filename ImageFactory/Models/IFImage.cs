﻿using BeatSaberMarkupLanguage.Animations;
using System;
using System.IO;
using UnityEngine;

namespace ImageFactory.Models
{
    public class IFImage
    {
        public readonly int width;
        public readonly int height;
        public readonly Sprite sprite;
        public readonly TimeSpan loadTime;
        public readonly Metadata metadata;
        public readonly AnimationControllerData? animationData;

        public IFImage(Sprite sprite, Metadata imageMetadata, TimeSpan timeToLoad)
        {
            this.sprite = sprite;
            loadTime = timeToLoad;
            metadata = imageMetadata;
            width = sprite.texture.width;
            height = sprite.texture.height;
        }

        public IFImage(AnimationControllerData animData, Metadata metadata, TimeSpan timeToLoad)
        {
            loadTime = timeToLoad;
            animationData = animData;
            this.metadata = metadata;
            sprite = animData.Sprites[0];
            width = (int)animData.Sprites[0].rect.width;
            height = (int)animData.Sprites[0].rect.height;
        }

        public struct Metadata
        {
            public readonly long size;
            public readonly FileInfo file;
            public readonly IFSaveData? saveData;
            public readonly AnimationFormat? animationType;

            private const string gifExtension = ".gif";
            private const string apngExtension = ".apng";

            public Metadata(FileInfo info, IFSaveData? imageSaveData = null)
            {
                file = info;
                size = info.Length;
                saveData = imageSaveData;

                string extension = Path.GetExtension(file.FullName);
                animationType = extension switch
                {
                    gifExtension => AnimationFormat.GIF,
                    apngExtension => AnimationFormat.APNG,
                    _ => null,
                };
            }
        }
    }
}