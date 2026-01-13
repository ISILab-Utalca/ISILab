using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class RemoveGraphNode : LBSManipulator
    {
        private QuestGraph _questGraph;
        private QuestBehaviour _behaviour;

        protected override string IconGuid => "ce08b36a396edbf4394f7a4e641f253d";

        public RemoveGraphNode()
        {
            Name = "Remove Quest Node";
            Description = "Click on a quest node to remove it.";
        }
        
        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            
            _questGraph = layer.GetModule<QuestGraph>();
            _behaviour = layer.GetBehaviour<QuestBehaviour>();
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            var node = _questGraph.GetNodeAtPosition<GraphNode>(endPosition);
            if (node == null) return;

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Remove Quest Node");

            _questGraph.RemoveQuestNode(node);

            OnManipulationEnd?.Invoke();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }
    }
}
