using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public class GrammarFieldEditor : LBSCustomEditor
    {
        private LBSCustomLabel nameLabel;

        // field umxl are added here
        protected VisualElement content;

        public GrammarFieldEditor(object target) : base(target)
        {
            SetNewInfo(target);
        }

        public override void SetInfo(object paramTarget)
        {
            target = paramTarget as GrammarField;
        }

        public void SetNewInfo(object paramTarget)
        {
            SetInfo(paramTarget);
            CreateVisualElement();
        }

        protected override VisualElement CreateVisualElement()
        {
            Clear();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarFieldEditor");
            visualTree.CloneTree(this);

            nameLabel = this.Q<LBSCustomLabel>("Name");
            var name = (target as GrammarField)?.name;
            nameLabel.text = name;

            content = this.Q<VisualElement>("Content");
            return this;
        }
    }
}