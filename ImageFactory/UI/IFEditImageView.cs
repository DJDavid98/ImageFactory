﻿using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using ImageFactory.Managers;
using ImageFactory.Models;
using IPA.Utilities;
using SiraUtil.Logging;
using System;
using UnityEngine;
using UnityEngine.UI;
using VRUIControls;
using Zenject;

namespace ImageFactory.UI
{
    [ViewDefinition("ImageFactory.Views.edit-image-view.bsml")]
    [HotReload(RelativePathToLayout = @"..\Views\edit-image-view.bsml")]
    internal class IFEditImageView : BSMLAutomaticViewController
    {
        public event Action? Cancelled;
        public event Action? Saved;

        private Config _config = null!;
        private SiraLog _siraLog = null!;
        private DiContainer _container = null!;
        private ViewController _dummyView = null!;
        private ImageManager _imageManager = null!;
        private FloatingScreen _floatingScreen = null!;
        private InputFieldView _editorFieldView = null!;
        private InputFieldView _templateFieldView = null!;
        private ImageEditorManager _imageEditorManager = null!;
        private PhysicsRaycasterWithCache _cacheRaycaster = null!;

        [UIAction("cancel-clicked")]
        protected void CancelClicked()
        {
            _presentationHost.Reset();
            Cancelled?.Invoke();
        }

        [UIAction("save-clicked")]
        protected void SaveClicked()
        {
            _imageEditorManager.Dismiss(true);
            _presentationHost.Reset();
            Saved?.Invoke();
        }

        [UIComponent("input-root")]
        protected readonly RectTransform _inputRoot = null!;

        [UIValue("presentation-host")]
        protected PresentationHost _presentationHost = null!;

        [UIValue("enabled")]
        protected bool Enabled
        {
            get => _imageEditorManager.Enabled;
            set { _imageEditorManager.Enabled = value; NotifyPropertyChanged(); }
        }

        [UIValue("scale-x")]
        protected float XScale
        {
            get => _imageEditorManager.Size.x;
            set { _imageEditorManager.Size = new Vector2(value, _imageEditorManager.Size.y); NotifyPropertyChanged(); _floatingScreen.handle.transform.localScale = Vector3.one / 5f * _imageEditorManager.Size.x; }
        }

        [UIValue("scale-y")]
        protected float YScale
        {
            get => _imageEditorManager.Size.y;
            set { _imageEditorManager.Size = new Vector2(_imageEditorManager.Size.x, value); NotifyPropertyChanged(); _floatingScreen.handle.transform.localScale = Vector3.one / 5f * _imageEditorManager.Size.x; }
        }

        [UIValue("src-blend")]
        protected float SourceBlend
        {
            get => _imageEditorManager.SourceBlend;
            set { _imageEditorManager.SourceBlend = value; NotifyPropertyChanged(); }
        }

        [UIValue("dst-blend")]
        protected float DestinationBlend
        {
            get => _imageEditorManager.DestinationBlend;
            set { _imageEditorManager.DestinationBlend = value; NotifyPropertyChanged(); }
        }

        [Inject]
        public void Construct(Config config, SiraLog siraLog, DiContainer container, ImageManager imageManager, ImageEditorManager imageEditorManager, PhysicsRaycasterWithCache cacheRaycaster, LevelSearchViewController levelSearchViewController)
        {
            _config = config;
            _siraLog = siraLog;
            _container = container;
            _imageManager = imageManager;
            _cacheRaycaster = cacheRaycaster;
            _imageEditorManager = imageEditorManager;
            _presentationHost = container.Instantiate<PresentationHost>();
            _templateFieldView = levelSearchViewController.GetField<InputFieldView, LevelSearchViewController>("_searchTextInputFieldView");
        }

        private void CreateScreen()
        {
            if (_floatingScreen == null)
            {
                _siraLog.Info("Starting...");
                _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(10, 10f), true, Vector3.zero, Quaternion.identity, 0f, false);
                _floatingScreen.GetComponent<VRGraphicRaycaster>().SetField("_physicsRaycaster", _cacheRaycaster);
                _dummyView = BeatSaberUI.CreateViewController<ViewController>();
                _floatingScreen.transform.localScale = Vector3.one;
                _dummyView.name = "IF Editor DummyViewController";
                _floatingScreen.name = "IF Editor Cube";
                _floatingScreen.gameObject.SetActive(false);
            }
        }

        public void EnableEditing(IFImage image, IFSaveData? data = null)
        {
            CreateScreen();
            _presentationHost.Reset();
            _imageManager.DeanimateAll();
            _presentationHost.LastData = data?.Presentation;
            var saveData = data ?? new IFSaveData { Enabled = true, Position = new Vector3(0f, 2f, 2f), Name = image.metadata.file.Name, LocalFilePath = image.metadata.file.Name };
            bool isNew = data == null;

            // Uses our dummy view controller to use the BSML handle because I am lazy
            // Then, resize the handle accordingly, setup our editor sprite instance,
            // set the position of our handle TO the image and THEN make the sprite a
            // child of the handle screen.
            _floatingScreen.gameObject.SetActive(true);
            _floatingScreen.SetRootViewController(_dummyView, AnimationType.None);
            _floatingScreen.Handle.transform.localScale = Vector3.one / 5f * saveData.Size;
            _floatingScreen.Handle.gameObject.transform.localPosition = Vector3.zero;
            Transform tForm = _imageEditorManager.Present(image, saveData, clone =>
            {
                var val = _presentationHost.Export();

                saveData.Name = clone.Name;
                saveData.Size = clone.Size;
                saveData.Enabled = clone.Enabled;
                saveData.Position = clone.Position;
                saveData.Rotation = clone.Rotation;
                saveData.SourceBlend = clone.SourceBlend;
                saveData.DestinationBlend = clone.DestinationBlend;
                saveData.Presentation.PresentationID = val.Item1;
                saveData.Presentation.Duration = val.Item3 ?? 0f;
                saveData.Presentation.Value = val.Item2;
                if (string.IsNullOrWhiteSpace(saveData.Presentation.PresentationID))
                    saveData.Presentation.PresentationID = Presenters.ScenePresenter.EVERYWHERE_ID;

                if (isNew)
                    _config.SaveData.Add(saveData);
                _config.Changed();

                // Tells the image manager to send an event out to allow everything else to update their image states.
                _imageManager.UpdateImage(image, saveData, isNew ? ImageUpdateArgs.Action.Added : ImageUpdateArgs.Action.Updated);
            });
            _floatingScreen.ScreenPosition = _imageEditorManager.Position;
            _floatingScreen.ScreenRotation = _imageEditorManager.Rotation;
            _floatingScreen.Handle.gameObject.transform.localPosition = Vector3.zero;
            _floatingScreen.Handle.gameObject.transform.position = saveData.Position;
            tForm.transform.SetParent(_floatingScreen.transform, true);
            _editorFieldView.SetText(_imageEditorManager.Name);
            Enabled = Enabled;
            XScale = XScale;
            YScale = YScale;
            SourceBlend = SourceBlend;
            DestinationBlend = DestinationBlend;
            _presentationHost.Update();
        }
        private void NameFieldUpdated(InputFieldView field)
        {
            _imageEditorManager.Name = field.text;
        }

        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            _editorFieldView.onValueChanged.AddListener(NameFieldUpdated);
        }

        public override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _presentationHost.Reset();
            _imageManager.ReanimateAll();
            _imageEditorManager.Dismiss(false);
            _floatingScreen.SetRootViewController(null, AnimationType.None);
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            _editorFieldView.onValueChanged.RemoveListener(NameFieldUpdated);
            _floatingScreen.gameObject.SetActive(false);
        }

        [UIAction("#post-parse")]
        protected void Parsed()
        {
            _siraLog.Info("did parse");
            _inputRoot.GetComponent<ContentSizeFitter>().enabled = false;
            _editorFieldView = _container.InstantiatePrefabForComponent<InputFieldView>(_templateFieldView.gameObject, _inputRoot.transform);
            _editorFieldView.SetField("_keyboardPositionOffset", new Vector3(0f, -20f, 0f));
            _editorFieldView.SetField("_textLengthLimit", 48);
            _editorFieldView.SetText(_imageEditorManager.Name);
        }
    }
}
