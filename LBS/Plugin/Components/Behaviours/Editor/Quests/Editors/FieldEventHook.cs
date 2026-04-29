using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Analytics.IAnalytic;

namespace ISILab.LBS.VisualElements
{
    public class FieldEventHook : GrammarFieldEditor
    {
        private VisualElement _onEventCompleteVe;
        private LBSCustomEventHooker _hooker;

        public FieldEventHook(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("FieldEventHook");
            visualTree.CloneTree(content);

            _onEventCompleteVe = this.Q<VisualElement>("EventComplete");
            _hooker = this.Q<LBSCustomEventHooker>("EventHooker");
            _hooker.EventType = LBSEventType.Complete;
            _hooker.AllowChangeTriggerEnable = false;

            _hooker.Hooker = (target as GrammarEventHook).value;
            _hooker.Selector.allowSceneObjects = true;
            _hooker.RefreshMethodList();
            return this;
        }
    }
}