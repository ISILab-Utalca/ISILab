using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
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
        public static BlueprintPanel Instance => _instance;

        #endregion

        #region CONSTRUCTORS
        public BlueprintPanel() : base()
        {
            _instance = this;

            visualTreeAsset ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("BlueprintPanel");
            visualTreeAsset.CloneTree(this);
            name = "BlueprintPanel";

            deleteButton = this.Q<LBSCustomButton>("DeleteButton");
            cpatureButton = this.Q<LBSCustomButton>("CaptureButton");
            scrollView = this.Q<ScrollView>("BlueprintScrollView");

            deleteButton.clicked += OnDeleteButtonClicked;
            cpatureButton.clicked += OnCaptureButtonClicked;

            pickingMode = PickingMode.Ignore;
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
            ISILab.LBS.Components.Blueprint newInstance = ScriptableObject.CreateInstance<ISILab.LBS.Components.Blueprint>();

            // --- Count by type ---
            Dictionary<Type, int> typeCounter = new();
            foreach(var co in capturedObjects)
            {
                if (typeCounter.ContainsKey(co.GetType())) typeCounter[co.GetType()]++;
                else typeCounter.Add(co.GetType(), 1);
            }


            foreach(KeyValuePair<Type, int> tc in typeCounter)
            {
                Debug.Log("Type:" + tc.Key.ToString() + "|| Count:" + tc.Value);
            }
        }


        public void SetPreviewTexture(Texture2D tex)
        {
            BlueprintEntry blueprintEntry = this.Q<BlueprintEntry>();
            blueprintEntry.BlueprintImage = tex;

        }

        #endregion
    }
}
