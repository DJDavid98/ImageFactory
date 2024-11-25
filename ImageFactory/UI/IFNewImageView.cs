﻿using BeatSaberMarkupLanguage.Animations;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using ImageFactory.Managers;
using ImageFactory.Models;
using SiraUtil.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ImageFactory.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\new-image-view.bsml")]
    [ViewDefinition("ImageFactory.Views.new-image-view.bsml")]
    internal class IFNewImageView : BSMLAutomaticViewController
    {
        public event Action<IFImage>? NewImageRequested;

        #region Injected Dependencies

        protected TimeTweeningManager _tweeningManager = null!;

        protected MetadataStore _metadataStore = null!;

        protected ImageManager _imageManager = null!;

        protected SiraLog _siraLog = null!;

        [Inject]
        protected void Construct(DiContainer container, ImageManager imageManager, MetadataStore metadataStore, TimeTweeningManager TimeTweeningManager, SiraLog siraLog)
        {
            _selectImageModalHost = container.Instantiate<SelectImageModalHost>();
            _tweeningManager = TimeTweeningManager;
            _metadataStore = metadataStore;
            _imageManager = imageManager;
            _siraLog = siraLog;
        }

        #endregion

        #region Canvas Animator

        [UIComponent("selection-root")]
        protected readonly RectTransform _selectionRoot = null!;
        protected CanvasGroup _selectionCanvas = null!;

        [UIComponent("loading-root")]
        protected readonly RectTransform _loadingRoot = null!;
        protected CanvasGroup _loadingCanvas = null!;

        public async Task AnimateToSelectionCanvas()
        {
            const float animationSector = 0.4f;

            _loadingCanvas.alpha = 1f;
            _loadingCanvas.gameObject.SetActive(true);
            _tweeningManager.KillAllTweens(_loadingCanvas);
            _tweeningManager.AddTween(new FloatTween(1f, 0f, val =>
            {
                _loadingCanvas.alpha = val;
            }, animationSector, EaseType.InOutQuad), _loadingCanvas);

            await Task.Delay((int)(animationSector * 1000));
            _loadingCanvas.gameObject.SetActive(false);

            _selectionCanvas.alpha = 0f;
            _selectionCanvas.gameObject.SetActive(true);
            _tweeningManager.KillAllTweens(_selectionCanvas);
            _tweeningManager.AddTween(new FloatTween(0f, 1f, val =>
            {
                _selectionCanvas.alpha = val;
            }, animationSector, EaseType.InOutQuad), _selectionCanvas);
        }

        #endregion

        #region Image Loader

        [UIValue("select-image-modal-host")]
        protected SelectImageModalHost _selectImageModalHost = null!;

        [UIComponent("image-list")]
        protected readonly CustomCellListTableData _imageList = null!;
        private bool _didLoad = false;

        [UIComponent("up-button")]
        protected readonly Button _upbutton = null!;

        [UIComponent("down-button")]
        protected readonly Button _downButton = null!;

        public async Task LoadImages()
        {
            if (_didLoad)
                return;

            foreach (var metadata in _metadataStore.AllMetadata())
                await _imageManager.LoadImage(metadata);

            var loadedImages = _imageManager.LoadedImages().OrderBy(i => i.metadata.file.Name);
            await AnimateToSelectionCanvas();
            _didLoad = true;

            var data = loadedImages.Select(image => new NewImageCell(image, ClickedImageCell)).ToList();
            Utilities.InitializeCustomCellTableviewData(_imageList, data, _siraLog);
            _imageList.TableView.ReloadData();
        }

        private void ClickedImageCell(IFImage image)
        {
            _selectImageModalHost.Present(image, ClickedImageCreate);
        }

        private void ClickedImageCreate(IFImage image)
        {
            NewImageRequested?.Invoke(image);
        }

        #endregion

        [UIAction("#post-parse")]
        protected void Parsed()
        {
            _selectionCanvas = _selectionRoot.gameObject.AddComponent<CanvasGroup>();
            _loadingCanvas = _loadingRoot.gameObject.AddComponent<CanvasGroup>();
            //_imageList.tableView.SetButtons(_upbutton, _downButton);
            _ = LoadImages();
        }

        private class NewImageCell
        {
            public readonly IFImage image;
            public readonly Action<IFImage> createAction;

            [UIComponent("preview")]
            protected readonly ImageView _previewImage = null!;

            [UIComponent("file-name")]
            protected readonly CurvedTextMeshPro _fileName = null!;

            public NewImageCell(IFImage image, Action<IFImage> createClicked)
            {
                this.image = image;
                createAction = createClicked;
            }

            [UIAction("#post-parse")]
            protected void Parsed()
            {
                _previewImage.sprite = image.sprite;
                if (image.animationData != null)
                {
                    var stateUpdater = _previewImage.gameObject.AddComponent<AnimationStateUpdater>();
                    image.animationData.ActiveImages.Add(_previewImage);
                    stateUpdater.Image = _previewImage;
                }
                else
                {
                    _previewImage.material = Utilities.UINoGlowRoundEdge;
                }
                _fileName.text = image.metadata.file.Name;
            }

            [UIAction("clicked-create-button")]
            protected void ClickedCreateButton()
            {
                createAction?.Invoke(image);
            }
        }
    }
}
