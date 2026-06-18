using ISILab.LBS;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using UnityEngine;
using UnityEngine.UIElements;

[LBSCustomEditor("BSP Dungeon Generator", typeof(BSPDungeonAssistant))]
public class BSPDungeonAssistantEditor : LBSCustomEditor
{
    BSPDungeonAssistant assistant;

    public BSPDungeonAssistantEditor(object target) : base(target)
    {
        assistant = (BSPDungeonAssistant) target;
        //assistant.OnDetach = () => LBSInspectorPanel.Instance.UnregisterCallback<GeometryChangedEvent>(SetLayoutCallback);
        CreateVisualElement();
    }

    public override void SetInfo(object paramTarget)
    {
        assistant = paramTarget as BSPDungeonAssistant;
    }

    protected override VisualElement CreateVisualElement()
    {
        var button = new LBSCustomButton() { text = "Run" };
        RegisterCallback<ClickEvent>(_evt => assistant.Run());
        this.Add(button);
        return this;
    }
}
