using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.VisualElements;
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
        private Graph _questGraph;
        private QuestBehaviour _behaviour;

        protected override string IconGuid => "ce08b36a396edbf4394f7a4e641f253d";

        public RemoveGraphNode()
        {
            Name = "Remove Quest Node";
            Description = "Click on a quest node to remove it.";
            groupWeight = 0;
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            
            _questGraph = layer.GetModule<Graph>();
            _behaviour = layer.GetBehaviour<QuestBehaviour>();
        }

        public void Delete(object obj)
        {
            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Remove Quest Node");

            _questGraph.RemoveNode(obj);

            OnManipulationEnd?.Invoke();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }
    }
}
