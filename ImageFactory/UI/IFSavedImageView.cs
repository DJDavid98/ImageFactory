using BeatSaberMarkupLanguage.Animations;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using ImageFactory.Managers;
using ImageFactory.Models;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ImageFactory.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\saved-image-view.bsml")]
    [ViewDefinition("ImageFactory.Views.saved-image-view.bsml")]
    internal class IFSavedImageView : BSMLAutomaticViewController
    {
        public event Action<IFImage, IFSaveData>? EditImageRequested;
        public bool ShouldRefresh { get; set; }

        #region Injected Dependencies

        protected TimeTweeningManager _tweeningManager = null!;

        protected MetadataStore _metadataStore = null!;

        protected ImageManager _imageManager = null!;

        protected Config _config = null!;

        protected SiraLog _siraLog = null!;

        [Inject]
        protected void Construct(Config config, ImageManager imageManager, MetadataStore metadataStore, TimeTweeningManager TimeTweeningManager, SiraLog siraLog)
        {
            _tweeningManager = TimeTweeningManager;
            _metadataStore = metadataStore;
            _imageManager = imageManager;
            _config = config;
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
            _selectionCanvas.gameObject.SetActive(false);
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

        [UIComponent("image-list")]
        protected readonly CustomCellListTableData _imageList = null!;

        [UIComponent("up-button")]
        protected readonly Button _upbutton = null!;

        [UIComponent("down-button")]
        protected readonly Button _downButton = null!;

        public async Task LoadImages()
        {
            foreach (var image in _imageList.Data.Cast<EditImageCell>())
            {
                if (image.stateUpdater != null && image.previewImage != null)
                {
                    image.image.animationData!.ActiveImages.Remove(image.previewImage);
                    image.stateUpdater.Image = null;
                }
            }
            foreach (Transform pres in _imageList.TableView.contentTransform)
                Destroy(pres.gameObject);

            _imageList.Data.Clear();
            _imageList.TableView.ReloadData();

            var data = new List<EditImageCell>();
            foreach (var save in _config.SaveData)
            {
                IFImage.Metadata? metadata = _imageManager.GetMetadata(save);
                if (metadata.HasValue)
                {
                    var image = await _imageManager.LoadImage(metadata.Value);
                    if (image != null)
                        data.Add(new EditImageCell(image, save, ClickedImageEdit, ClickedImageDelete));
                }
            }
            Utilities.InitializeCustomCellTableviewData(_imageList, data, _siraLog);
            await AnimateToSelectionCanvas();
            _imageList.TableView.ReloadData();
        }

        private void ClickedImageDelete(IFImage image, IFSaveData saveData)
        {
            _config.SaveData.Remove(saveData);
            _imageManager.UpdateImage(image, saveData, ImageUpdateArgs.Action.Removed);
            _config.Changed();
        }

        private void ClickedImageEdit(IFImage image, IFSaveData saveData)
        {
            EditImageRequested?.Invoke(image, saveData);
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

        protected void Start()
        {
            _imageManager.ImageUpdated += ImageManager_ImageUpdated;
        }

        private void ImageManager_ImageUpdated(object sender, ImageUpdateArgs e)
        {
            if (ShouldRefresh)
                _ = LoadImages();
        }

        public override void OnDestroy()
        {
            _imageManager.ImageUpdated -= ImageManager_ImageUpdated;
            base.OnDestroy();
        }

        private class EditImageCell
        {
            public readonly IFImage image;
            public readonly IFSaveData saveData;
            public readonly Action<IFImage, IFSaveData> editAction;
            public readonly Action<IFImage, IFSaveData> deleteAction;
            public AnimationStateUpdater stateUpdater = null!;

            [UIComponent("preview")]
            public readonly ImageView previewImage = null!;

            [UIComponent("save-name")]
            protected readonly CurvedTextMeshPro _saveName = null!;

            public EditImageCell(IFImage image, IFSaveData saveData, Action<IFImage, IFSaveData> editClicked, Action<IFImage, IFSaveData> deleteClicked)
            {
                this.image = image;
                this.saveData = saveData;
                editAction = editClicked;
                deleteAction = deleteClicked;
            }

            [UIAction("#post-parse")]
            protected void Parsed()
            {
                previewImage.sprite = image.sprite;
                if (image.animationData != null)
                {
                    stateUpdater = previewImage.gameObject.AddComponent<AnimationStateUpdater>();
                    image.animationData.ActiveImages.Add(previewImage);
                    stateUpdater.Image = previewImage;
                }
                else
                {
                    previewImage.material = Utilities.UINoGlowRoundEdge;
                }
                _saveName.text = saveData.Name;
                _saveName.color = saveData.Enabled ? Color.green : Color.red;
            }

            [UIAction("clicked-delete-button")]
            protected void ClickedDeleteButton()
            {
                deleteAction?.Invoke(image, saveData);
            }

            [UIAction("clicked-edit-button")]
            protected void ClickedEditButton()
            {
                editAction?.Invoke(image, saveData);
            }
        }
    }
}
