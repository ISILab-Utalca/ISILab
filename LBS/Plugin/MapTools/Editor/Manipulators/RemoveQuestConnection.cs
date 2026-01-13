using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using LBS.Components;
using log4net.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

namespace ISILab.LBS.Manipulators
{
    public class RemoveQuestConnection : LBSManipulator
    {
        private QuestGraph _questGraph;
        private QuestBehaviour _behaviour;

        protected override string IconGuid => "b534f3f3d94bf1349babd81aa035d583";

        public RemoveQuestConnection()
        {
            Name = "Remove Quest Connection";
            Description = "Click a connection line between nodes to remove it.";
        }
        
        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            
            _questGraph = layer.GetModule<QuestGraph>();
            _behaviour = layer.GetBehaviour<QuestBehaviour>();
        }
        
        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            QuestEdge edge = _questGraph.GetEdge(endPosition, 10);

            if (edge == null) 
            {
                LBSMainWindow.MessageNotify("No Edge Selected to Remove", LogType.Error, 5);
                return; 
            }

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Remove Quest Connection");

            _questGraph.RemoveEdge(edge);
            OnManipulationEnd.Invoke();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }
    }
}