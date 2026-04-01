using System;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using LBS.Components;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using ISILab.LBS.Plugin.Core.Settings;

namespace ISILab.LBS.Manipulators
{
    public class AddGraphNode : LBSManipulator
    {
        private QuestGraph _questGraph;
        private QuestBehaviour _behaviour;

        protected override string IconGuid => "3d0b251f4a09bce4b9224787cfa08d49";

        public AddGraphNode()
        {
            Name = "Add Graph Node";
            Description = "Pick an action or branch from the inspector panel, then Click on the graph.";
        }

        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            
            _questGraph = layer.GetModule<QuestGraph>();
            _behaviour = layer.GetBehaviour<QuestBehaviour>();
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            if (_behaviour.activeGraphNodeType == null)
            {
                LBSMainWindow.MessageNotify(
                    new LBSLog("Can't add node. Make sure a node is selected from the behaviour panel.", LogType.Error, 5));
                return;
            }
            if (_behaviour.activeGraphNodeType == typeof(QuestNode) && _behaviour.ActionToSet == string.Empty)
            {
                LBSMainWindow.MessageNotify(
                    new LBSLog("Can't add node. Make sure to select a grammar and a word.", LogType.Error, 5));
                return;
            }

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Add Quest Node");

            var newNode = _questGraph.AddNewNode(_behaviour,endPosition);
            _behaviour.SelectedGraphNode = newNode;

            OnManipulationEnd.Invoke();
            e.StopImmediatePropagation();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }
    }
}