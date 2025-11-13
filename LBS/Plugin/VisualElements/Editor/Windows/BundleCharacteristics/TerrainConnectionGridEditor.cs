using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.Macros;
using LBS.Bundles;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Internal;
using ISILab.LBS;
using ISILab.LBS.VisualElements;
using ISILab.LBS.Editor;
using static ISILab.LBS.Modules.ConnectedTileMapModule;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("Weights", typeof(LBSTerrainConnectionGrid))]
    public class TerrainConnectionGridEditor : LBSCustomEditor
    {
        private Button openGridEditorWindow;
        private static TerrainConnectionGridEditorWindow gridEditorWindow;

        public TerrainConnectionGridEditor()
        {

        }
        public TerrainConnectionGridEditor(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);

        }

        public override void SetInfo(object _paramTarget)
        {
            this.target = _paramTarget;
        }

        protected override VisualElement CreateVisualElement()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TerrainConnectionGridEditor");
            visualTree.CloneTree(this);

            openGridEditorWindow = this.Q<Button>("OpenGridEditorButton");
            openGridEditorWindow.clicked += OpenGridEditorWindow;

            return this;
        }

        private void OpenGridEditorWindow()
        {
            if (gridEditorWindow)
                gridEditorWindow.Close();

            gridEditorWindow = ScriptableObject.CreateInstance<TerrainConnectionGridEditorWindow>();
            gridEditorWindow.connectionGridTarget = target as LBSTerrainConnectionGrid;

            gridEditorWindow.Show();
        }
    }
}