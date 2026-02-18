using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    [UxmlElement]
    public partial class BlueprintPanel : VisualElement
    {
        #region VIEW ELEMENTS
        LBSCustomButton deleteButton;
        LBSCustomButton cpatureButton;
        ScrollView scrollView;
        
        static VisualTreeAsset visualTreeAsset;
        #endregion

        #region FIELDS
        public object selectedArea;
        public ISILab.LBS.Components.Blueprint selectedBlueprint;
        private CaptureInArea _captureArea;

        #endregion

        #region PROPERTIES
        public CaptureInArea CaptureManipulator
        {
            set
            {
                _captureArea = value;
            }
        }
        #endregion

        #region STATIC METHODS

        private static BlueprintPanel _instance;
        public static BlueprintPanel Instance
        {
            get
            {
                return _instance;
            }
            set
            {
                if (_instance is null) _instance = value;
            }
        }
        #endregion

        #region CONSTRUCTORS
        public BlueprintPanel() : base()
        {
            Instance = this;

            visualTreeAsset ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("BlueprintPanel");
            visualTreeAsset.CloneTree(this);
            name = "BlueprintPanel";

            deleteButton = this.Q<LBSCustomButton>("DeleteButton");
            cpatureButton = this.Q<LBSCustomButton>("CaptureButton");
            scrollView = this.Q<ScrollView>("BlueprintScrollView");

            deleteButton.clicked += OnDeleteButtonClicked;
            cpatureButton.clicked += OnCaptureButtonClicked;

            this.pickingMode = PickingMode.Ignore; 

        }

        private void OnDeleteButtonClicked()
        {
            if (selectedBlueprint is null) return;

            ScriptableObject.DestroyImmediate(selectedBlueprint);
            selectedBlueprint = null;
        }

        private void OnCaptureButtonClicked()
        {
            if (_captureArea is null) return;
            var capturedObjects = _captureArea.capturedObjects;
            if (capturedObjects.Length == 0) return;

            object[] objs = new object[] { selectedArea ?? new object() };
            var newInstance = ScriptableObject.CreateInstance<ISILab.LBS.Components.Blueprint>();

            // --- Count by type ---
            Dictionary<Type, int> typeCounter = new();
            foreach(var co in capturedObjects)
            {
                if (typeCounter.ContainsKey(co.GetType())) typeCounter[co.GetType()]++;
                else typeCounter.Add(co.GetType(), 1);
            }


            foreach(var tc in typeCounter)
            {
                Debug.Log("Type:" + tc.Key.ToString() + "|| Count:" + tc.Value);
            }
        }


        public void SetPreviewTexture(Texture2D tex)
        {
            VisualElement preview = new VisualElement();
            preview.style.backgroundImage = new StyleBackground(tex);
            Add(preview);
        }


        #endregion

        public static void CaptureElement(VisualElement element, Rect localRect, Action<Texture2D> done)
        {
            var panel = element.panel;
            if (panel == null)
                return;

            // Convert element-local → screen space
            Vector2 min = localRect.min;
            Vector2 max = localRect.max;

            Rect screenRect = new Rect(min, max - min);

            // wait for repaint
            EditorApplication.delayCall += () =>
            {
                Debug.Log((int)screenRect.width + "," + (int)screenRect.height);
                var tex = new Texture2D((int)screenRect.width, (int)screenRect.height, TextureFormat.RGBA32, false);

                tex.ReadPixels(new Rect(
                    screenRect.x,
                    Screen.height - screenRect.y - screenRect.height,
                    screenRect.width,
                    screenRect.height), 0, 0);

                tex.Apply();

                done?.Invoke(tex);
            };
        }
    }
}
