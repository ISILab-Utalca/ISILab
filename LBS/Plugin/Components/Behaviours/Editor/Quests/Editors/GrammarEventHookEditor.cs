using ISILab.AI.Grammar;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Data;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [GrammarFieldEditor(typeof(GrammarEventHook))]
    public class GrammarEventHookEditor : GrammarFieldEditor
    {
        private VisualElement _onEventCompleteVe;
        private LBSCustomEventHooker _hooker;

        public GrammarEventHookEditor(object target) : base(target)
        {
        }

        public override void SetInfo(object paramTarget)
        {
            base.SetInfo(paramTarget);
        }

        protected override VisualElement CreateVisualElement()
        {
            base.CreateVisualElement();

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("GrammarEventHookEditor");
            visualTree.CloneTree(content);

            _onEventCompleteVe = this.Q<VisualElement>("EventComplete");
            _hooker = this.Q<LBSCustomEventHooker>("EventHooker");
            _hooker.EventType = LBSEventType.Complete;
            _hooker.AllowChangeTriggerEnable = false;

            _hooker.Hooker = (target as GrammarEventHook).value;
            _hooker.Selector.allowSceneObjects = true;
            _hooker.RefreshMethodList();

            (target as GrammarField).Refresh = () =>
            {
                _hooker.RefreshMethodList();
            };

            return this;
        }
    }
}