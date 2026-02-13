using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleManager;
using ISILab.LBS.VisualElements;
using LBS;
using LBS.VisualElements;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint
{
    [UxmlElement]
    public partial class BlueprintPanel : LBSCustomEditor, IToolProvider
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

            // Make the manipulator to pick the capture area
            SetTools(ToolKit.Instance);

        }

        private void OnDeleteButtonClicked()
        {
            if (selectedBlueprint is null) return;

            ScriptableObject.DestroyImmediate(selectedBlueprint);
            selectedBlueprint = null;
        }

        private void OnCaptureButtonClicked()
        {
            object[] objs = new object[] { selectedArea ?? new object() };
            ScriptableObject.CreateInstance<ISILab.LBS.Components.Blueprint>();
        }

        public void SetTools(ToolKit toolkit)
        {
            _captureArea = new CaptureInArea();
            LBSTool t = new LBSTool(_captureArea);
            //t4.OnSelect += LBSInspectorPanel.ActivateBehaviourTab
            toolkit.ActivateTool(t, null, _captureArea);

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
