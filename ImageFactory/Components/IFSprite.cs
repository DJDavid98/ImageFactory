﻿using ImageFactory.Managers;
using ImageFactory.Models;
using IPA.Utilities.Async;
using SiraUtil.Logging;
using System;
using System.Threading.Tasks;
using Tweening;
using UnityEngine;
using Zenject;

namespace ImageFactory.Components
{
    internal class IFSprite : MonoBehaviour
    {
        private IFImage? _image;
        public const float ANIM_TIME = 0.35f;
        private RendererAnimationStateUpdater? _animator = null!;
        [SerializeField] private SpriteRenderer _spriteRenderer = null!;
        [Inject] private readonly TimeTweeningManager _tweeningManager = null!;
        [Inject] private readonly ResourceLoader _resourceLoader = null!;
        [Inject] private readonly SiraLog _siraLog = null!;

        // If we're updating the size of an animated image, we need to recalculate its position extents to remain centered.
        public Vector2 Size
        {
            get => new Vector2(transform.localScale.x, transform.localScale.y);
            set
            {
                if (value.x == 0)
                    value = new Vector2(0.01f, value.y);
                if (value.y == 0)
                    value = new Vector2(value.x, 0.01f);

                var position = Position;
                transform.localScale = new Vector3(value.x, value.y, 1f);
                if (_image != null && _image.animationData != null)
                {
                    Position = position;
                }
            }
        }

        // We are offsetting the position of the actual position with the sprite extents to make "0,0" in the center, not the corner. when working with animated images.
        public Vector3 Position
        {
            get
            {
                if (_image != null && _image.animationData != null && _spriteRenderer != null)
                {
                    var pos = transform.position + new Vector3(_spriteRenderer.bounds.extents.x, _spriteRenderer.bounds.extents.y, 0);
                    return pos;
                }
                else return transform.position;
            }
            set
            {
                if (_image != null && _image.animationData != null && _spriteRenderer != null)
                {
                    var pos = value - new Vector3(_spriteRenderer.bounds.extents.x, _spriteRenderer.bounds.extents.y, 0);
                    transform.position = pos;
                }
                else if (transform != null)
                    transform.position = value;
            }
        }

        public Quaternion Rotation
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }

        public IFImage? Image
        {
            get => _image;
            set
            {
                var oldPos = Position;
                _image = value;
                if (_image is null)
                {
                    _spriteRenderer.sprite = null!;
                    if (_animator != null)
                    {
                        _animator.enabled = false;
                    }
                }
                else
                {
                    if (_image.metadata.animationType is null)
                    {
                        if (_animator != null)
                        {
                            _animator.enabled = false;
                        }
                        _spriteRenderer.sprite = _image.sprite;
                    }
                    else
                    {
                        _spriteRenderer.sprite = _image.animationData!.Sprites[0];
                        if (_animator == null)
                        {
                            _animator = gameObject.AddComponent<RendererAnimationStateUpdater>();
                            _animator.Renderer = _spriteRenderer;
                        }
                        _animator.enabled = true;
                        _animator.ControllerData = _image.animationData;
                    }
                }
                Position = oldPos;
            }
        }

        public float SourceBlend { 
            get => _spriteRenderer.material.GetFloat("_SrcBlend");
            set => _spriteRenderer.material.SetFloat("_SrcBlend", value);
        }

        public float DestinationBlend
        { 
            get => _spriteRenderer.material.GetFloat("_DstBlend");
            set => _spriteRenderer.material.SetFloat("_DstBlend", value);
        }

        internal void Setup(SpriteRenderer renderer)
        {
            _spriteRenderer = renderer;
        }

        protected void Start()
        {
            UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
            {
                try
                {
                    // Save values set before the new material was loaded
                    var lastSourceBlend = SourceBlend;
                    var lastDestinationBlend = DestinationBlend;
                    _spriteRenderer.material = new Material(await _resourceLoader.LoadSpriteMaterial());
                    // Restore values
                    SourceBlend = lastSourceBlend;
                    DestinationBlend = lastDestinationBlend;
                }
                catch (Exception e)
                {
                    _siraLog.Error($"Failed to load sprite renderer material: {e.Message}, falling back to {_spriteRenderer.material.name}\n{e.StackTrace}");
                }
            });
        }

        protected void OnEnable()
        {
            if (_spriteRenderer != null)
            {
                if (_tweeningManager != null)
                    _tweeningManager.KillAllTweens(this);
                _spriteRenderer.enabled = true;
            }
        }

        protected void OnDisable()
        {
            if (_tweeningManager != null)
                _tweeningManager.KillAllTweens(this);
        }

        public void KillAllTweens()
        {
            if (_tweeningManager != null)
                _tweeningManager.KillAllTweens(this);
        }

        public void AnimateIn()
        {
            _tweeningManager.KillAllTweens(this);
            _spriteRenderer.enabled = true;
            var lockSize = Size;

            Size = new Vector2(lockSize.x, 0f);
            var tween = new FloatTween(0f, lockSize.y, val =>
            {
                Size = new Vector2(lockSize.x, val);
            }, ANIM_TIME, EaseType.OutCubic)
            {
                onCompleted = delegate () { Size = lockSize; },
                onKilled = delegate () { Size = lockSize; }
            };
            _tweeningManager.AddTween(tween, this);
        }

        public void AnimateOut()
        {
            _tweeningManager.KillAllTweens(this);
            var lockSize = Size;
            var tween = new FloatTween(lockSize.y, 0f, val =>
            {
                Size = new Vector2(lockSize.x, val);
            }, ANIM_TIME, EaseType.OutCubic)
            {
                onCompleted = delegate () { _ = LazyUpdater(lockSize); },
                onKilled = delegate () { _ = LazyUpdater(lockSize); }
            };
            _tweeningManager.AddTween(tween, this);
        }

        private async Task LazyUpdater(Vector2 lockSize)
        {
            _spriteRenderer.enabled = false;
            await Task.Yield();
            Size = lockSize;
        }

        public class Pool : MonoMemoryPool<IFSprite> { /*Initialize Pool Type*/ }
    }
}
