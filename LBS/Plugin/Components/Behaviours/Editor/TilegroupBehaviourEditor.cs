using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using LBS;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("TileGroupBehavior", typeof(TileGroupBehavior))]
    public class TileGroupBehaviorEditor : LBSCustomEditor, IToolProvider
    {
        #region STATICS
        private static readonly Dictionary<Type, Type> AddonEditorMap = new()
        {
            { typeof(Addon_Trigger), typeof(Addon_TriggerEditor) },
            { typeof(Addon_Patrol), typeof(Addon_PatrolEditor) },
            { typeof(Addon_Destruct), typeof(Addon_DestroyEditor) },
            { typeof(Addon_Interact), typeof(Addon_InteractEditor) },
            { typeof(Addon_Drop), typeof(Addon_DropEditor) },
            { typeof(Addon_Unlock), typeof(Addon_UnlockEditor) },
            { typeof(Addon_TriggerUnlock), typeof(Addon_TriggerUnlockEditor) }
        };
        #endregion

        #region FIELDS

        private TileGroupBehavior behaviour;
        private ConnectionPicker pickConnection;

        #endregion

        #region VIEW FIELD

        private VisualElement NoContent;
        private VisualElement Content;

        private LBSCustomLabelIcon SelectedHeader;
        public VisualElement AddonContainer;

        private static VisualTreeAsset visualTree { get; set; }

        #endregion

        #region CONSTRUCTORS
        public TileGroupBehaviorEditor(object target) : base(target)
        {

            behaviour = target as TileGroupBehavior;
            if (behaviour is null) return;

            CreateVisualElement();
            SetInfo(behaviour);
        }

        protected sealed override VisualElement CreateVisualElement()
        {
            visualTree ??= DirectoryTools.GetAssetByName<VisualTreeAsset>("TileGroupBehaviorEditor", true);
            visualTree.CloneTree(this);

            NoContent = this.Q<VisualElement>("NoContent");
            Content = this.Q<VisualElement>("Content");
            SelectedHeader = this.Q<LBSCustomLabelIcon>("SelectedHeader");
            AddonContainer = this.Q<VisualElement>("AddonContainer");

            return this;
        }

        #endregion

        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {
            behaviour = paramTarget as TileGroupBehavior;
            ActionExtensions.AddUnique(ref behaviour.OnSelectedChanged, OnSelectedChanged);
            UpdateTilebundle(behaviour.SelectedTilemap);
        }

        private void OnSelectedChanged(TileBundleGroup tilemap)
        {
            DrawManager.Instance.UpdateLayer(behaviour.OwnerLayer);
            UpdateTilebundle(tilemap);
        }

        private void UpdateTilebundle(TileBundleGroup group)
        {
            AddonContainer.Clear();

            // Toggle Visibility
            bool isValid = group?.BundleData?.Bundle != null;
            NoContent.style.display = isValid ? DisplayStyle.None : DisplayStyle.Flex;
            Content.style.display = isValid ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isValid) return;

            // Header Setup
            SelectedHeader.Icon = group.BundleData.Bundle.Icon;
            SelectedHeader.Label = group.BundleData.BundleName;

            // 2. Use your existing Addons list!
            // We iterate through whatever was built in BuildAddons
            foreach (var addon in group.Addons)
            {
                Type addonType = addon.GetType();

                if (AddonEditorMap.TryGetValue(addonType, out Type editorType))
                {
                    // Instantiate the editor and pass 'behaviour'
                    var editor = (VisualElement)Activator.CreateInstance(editorType, behaviour);
                    AddonContainer.Add(editor);
                }
            }
        }

        public override void OnUnfocus()
        {
            base.OnUnfocus();
        }

        public void SetTools(ToolKit toolkit)
        {
            pickConnection = new ConnectionPicker();
            LBSTool t1 = new LBSTool(pickConnection);
            t1.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
            toolkit.ActivateTool(t1, behaviour.OwnerLayer, behaviour);                    

            // context exclusive from the Tilemap Panel
            VisualElement toolButton = toolkit.GetToolButton(typeof(ConnectionPicker));
            toolButton.SetEnabled(false);
        }

        #endregion
    }
}
