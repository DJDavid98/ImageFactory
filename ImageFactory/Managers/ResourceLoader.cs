﻿using IPA.Loader;
using SiraUtil.Logging;
using SiraUtil.Zenject;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace ImageFactory.Managers
{
    internal class ResourceLoader : IDisposable
    {
        private AssetBundle? _bundle;
        private Material? _cachedMaterial;
        private readonly SiraLog _siraLog;
        private readonly Assembly _pluginAssembly;
        private const string RESOURCE_PATH = "ImageFactory.Resources.sprite.assetbundle";
        private bool _isProcessing = false;

        public ResourceLoader(SiraLog siraLog, UBinder<Plugin, PluginMetadata> metadataBinder)
        {
            _siraLog = siraLog;
            _pluginAssembly = metadataBinder.Value.Assembly;
        }

        public async Task<Material> LoadSpriteMaterial()
        {
            while (_isProcessing)
                await Task.Yield();

            if (_cachedMaterial != null)
                return _cachedMaterial;

            _isProcessing = true;
            using Stream stream = _pluginAssembly.GetManifestResourceStream(RESOURCE_PATH);
            using MemoryStream ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            if (_cachedMaterial != null)
                return _cachedMaterial;

            var bundle = AssetBundle.LoadFromMemoryAsync(ms.ToArray());
            while (!bundle.isDone)
                await Task.Yield();

            _bundle = bundle.assetBundle;
            var spriteReq = bundle.assetBundle.LoadAssetAsync<GameObject>("_Sprite");
            while (!spriteReq.isDone)
                await Task.Yield();

            _cachedMaterial = new Material(((GameObject)spriteReq.asset).GetComponent<Renderer>().material);
            _bundle.Unload(false);
            _isProcessing = false;
            return _cachedMaterial;
        }

        public void Dispose()
        {
            if (_bundle != null)
                _bundle.Unload(true);
        }
    }
}
