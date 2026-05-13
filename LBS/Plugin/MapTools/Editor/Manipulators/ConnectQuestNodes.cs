using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.VisualElements.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using ISILab.LBS.Behaviours;
using UnityEngine.EventSystems;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor;

namespace ISILab.LBS.Manipulators
{
    public class ConnectQuestNodes : LBSManipulator
    {
        private QuestGraph _quest;
        private QuestBehaviour _behaviour;

        /// <summary>
        ///  from where we click to make a connection
        /// </summary>
        private GraphNode _first;
        protected override string IconGuid => "ec280cec81783e94cb5df0b0b40dec7e";

        public ConnectQuestNodes()
        {
            Feedback = new ConnectedLine();
            Name = "Connect Quest Node";
            Description = "Click on a starting node, then release on the follow up node.";
        }
        
        public override void Init(LBSLayer layer, object provider = null)
        {
            base.Init(layer, provider);
            
            _quest = layer.GetModule<QuestGraph>();
            _behaviour = layer.GetBehaviour<QuestBehaviour>();
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int startPosition, MouseDownEvent e)
        {
            _first = GetNodeViewAtPosition(e.mousePosition)?.Node;
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            var secondView = GetNodeViewAtPosition(e.mousePosition);
            var second = secondView?.Node;

            if (_first == null || second == null)
            {
                LBSMainWindow.MessageNotify(new LBSLog("A connection requires two nodes.", LogType.Error)); 
                return; 
            }
            if (Equals(_first, second))
            {
                LBSMainWindow.MessageNotify(new LBSLog("A node cannot connect to itself.", LogType.Error)); 
                return;
            }
            // prevent duplicates
            if (_quest.GraphEdges.Any(e => e.From.Contains(_first) && Equals(e.To, second)))
            {
                LBSMainWindow.MessageNotify(new LBSLog("This connection already exists.", LogType.Error));
                return;
            }
            // check for looping connections
            if (_quest.IsLooped(_first, second, new HashSet<GraphNode>()))
            {
                LBSMainWindow.MessageNotify(new LBSLog("The destination is a root of this node.", LogType.Error));
                return;
            }
            // only branching nodes can be a To on multiple edges
            if (second is QuestNode && _first is QuestNode)
            {
                bool alreadyTarget = _quest.GraphEdges.Any(e => Equals(e.To, second));
                if (alreadyTarget)
                {
                    LBSMainWindow.MessageNotify(new LBSLog("Action Nodes can only be the destination of one edge. For multiple use Branching nodes", LogType.Error));
                    return;
                }
            }

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Add Quest Connection");

            var result = _quest.AddEdge(_first, second);
            LBSMainWindow.MessageNotify(new LBSLog(result.Item1, result.Item2, 4));

            OnManipulationEnd.Invoke();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
        }

   

        private QuestGraphNodeView GetNodeViewAtPosition(Vector2 screenMousePos)
        {

            var view = MainView.Instance;
            Vector2 panelPos = screenMousePos - view.panel.visualTree.worldBound.position;
            VisualElement picked = view.panel.Pick(panelPos);
            while (picked != null)
            {
                if (picked is QuestGraphNodeView nodeView)
                {
                    //Debug.Log("found: " + nodeView.Node.ID);
                    return nodeView; 
                }

                picked = picked.parent;
            }
           // Debug.Log("NOT FOUND");
            return null;
        }
    }
}