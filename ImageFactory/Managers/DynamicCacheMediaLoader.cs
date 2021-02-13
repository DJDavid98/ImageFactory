﻿using IPA.Loader;
using SiraUtil;
using SiraUtil.Zenject;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ImageFactory.Managers
{
    internal class DynamicCacheMediaLoader : ISpriteAsyncLoader
    {
        private readonly Assembly _assembly;
        private readonly WebClient _webClient;
        private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public DynamicCacheMediaLoader(WebClient webClient, UBinder<Plugin, PluginMetadata> metadataBinder)
        {
            _webClient = webClient;
             _assembly = metadataBinder.Value.Assembly;
        }

        public async Task<Sprite> LoadSpriteAsync(string path, CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(path, out Sprite? sprite))
            {
                return sprite;
            }

            if (path.StartsWith("http"))
            {
                var response = await _webClient.GetAsync(path, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null!;

                sprite = BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(response.ContentToBytes());
            }
            else
            {
                using Stream stream = _assembly.GetManifestResourceStream($"{nameof(ImageFactory)}.Resources." + path);
                using MemoryStream ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                sprite = BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(ms.ToArray());
            }

            if (_cache.ContainsKey(path))
                _cache.Add(path, sprite);


            return sprite;
        }
    }
}