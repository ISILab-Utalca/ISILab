using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.VisualElements.Editor.Windows.BundleDirectionsWindows;
using ISILab.Commons.Utility.Editor;
using UnityEditor.UIElements;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("Weights", typeof(LBSDirection))]
    public class LBSDirectionEditor : LBSCustomEditor
    {
        LBSCustomObjectField cField;
        LBSCustomObjectField[] fields;

        private Button openDirectionToolButton;
        private static BundleDirectionEditorWindow directionWindow;

        public LBSDirectionEditor()
        {
            
        }

        public LBSDirectionEditor(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }

        public override void SetInfo(object _paramTarget)
        {
            this.target = _paramTarget;
            LBSDirection target = _paramTarget as LBSDirection;

            if (target == null)
                return;
            
            target.Size = 4;
            var connections = target.Connections;

            cField.objectType = typeof(LBSTag);
            cField.value = DirectoryTools.GetAssetByName<LBSTag>(target.Center, true);
            cField.RegisterValueChangedCallback(evt =>
            {
                target.SetCenter(evt.newValue as LBSTag);
            });

            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].objectType = typeof(LBSTag); 
                Debug.Log($"[{fields[i].name}] objectType seteado a: {fields[i].objectType}");

                var tag = DirectoryTools.GetAssetByName<LBSTag>(connections[i]);

                fields[i].value = tag;

                var index = i;

                fields[i].RegisterValueChangedCallback(evt =>
                {
                    target.SetConnection(evt.newValue as LBSTag, index);
                });
            }

        }

        protected override VisualElement CreateVisualElement()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSDirectionEditor");
            visualTree.CloneTree(this);

            cField = this.Q<LBSCustomObjectField>(name: "Center");

            fields = new LBSCustomObjectField[4];
            fields[0] = this.Q<LBSCustomObjectField>(name: "Right");
            fields[1] = this.Q<LBSCustomObjectField>(name: "Up");
            fields[2] = this.Q<LBSCustomObjectField>(name: "Left");
            fields[3] = this.Q<LBSCustomObjectField>(name: "Down");

            openDirectionToolButton = this.Q<Button>("OpenDirectionToolButton");
            openDirectionToolButton.clicked += OpenDirectionTool;

            return this;
        }

        private void OpenDirectionTool()
        {
            if (directionWindow)
                directionWindow.Close();

            directionWindow = ScriptableObject.CreateInstance<BundleDirectionEditorWindow>();
            directionWindow.target = target as LBSDirection;

            directionWindow.Show();
        }
    }
}