using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    [UxmlElement]
    public partial class BlueprintPanel : LBSCustomEditor
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

        #region CONSTRUCTORS
        public BlueprintPanel() : base()
        {
            visualTreeAsset ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("BlueprintPanel");
            visualTreeAsset.CloneTree(this);
            name = "BlueprintPanel";

            deleteButton = this.Q<LBSCustomButton>("DeleteButton");
            cpatureButton = this.Q<LBSCustomButton>("CaptureButton");
            scrollView = this.Q<ScrollView>("BlueprintScrollView");

            deleteButton.clicked += OnDeleteButtonClicked;
            cpatureButton.clicked += OnCaptureButtonClicked;

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


        public override void SetInfo(object paramTarget)
        {
            throw new NotImplementedException();
        }

        protected override VisualElement CreateVisualElement()
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
