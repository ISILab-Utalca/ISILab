using System;
using ISILab.LBS.Assistants;
using ISILab.LBS.Editor.Windows;
using LBS.Components;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ISILab.LBS.VisualElements.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class WaveFunctionCollapseManipulator : ManipulateTeselation
    {
        private Vector2Int _cornerStart;

        private AssistantWFC _assistant;

        private CancellationTokenSource _currentTaskCts;
        
        protected override string IconGuid => "08c60bd0a76e4bb4dad11ebf18bca46e";

        public WaveFunctionCollapseManipulator()
        {
            Feedback.fixToTeselation = true;
            Name = "Wave Function Collapse";
            Description = "Select an area to generate new connections.";
        }

        public override void Init(LBSLayer layer, object provider)
        {
            base.Init(layer, provider);

            _assistant = provider as AssistantWFC;
            Feedback.TeselationSize = layer.TileSize;
            layer.OnTileSizeChange += (val) => Feedback.TeselationSize = val;
        }

        protected override void OnMouseDown(VisualElement element, Vector2Int position, MouseDownEvent e)
        {
            _cornerStart = position;
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int position, MouseUpEvent e)
        {
            base.OnMouseUp(element, position, e);

            //If esc key was pressed, cancel the operation
            if (ForceCancel)
            {
                ForceCancel = false;
                return;
            }

            var x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "WFC");

            var corners = _assistant.OwnerLayer.ToFixedPosition(_cornerStart, position);

            var positions = new List<Vector2Int>();
            for (int i = corners.Item1.x; i <= corners.Item2.x; i++)
            {
                for (int j = corners.Item1.y; j <= corners.Item2.y; j++)
                {
                    var selected = new Vector2Int(i, j);
                    positions.Add(selected);
                }
            }

            _assistant.Positions = positions;

            // No longer having empty tiles means overwrite is default
            //
            _assistant.OverrideValues = e.ctrlKey;
            
            RunTask();
        }

        private void OnExecutionEnd(string log,LogType type)
        {
            EditorApplication.delayCall += () =>
            {
                var x = LBSController.CurrentLevel;

                _assistant.OnTermination?.Invoke();
                LBSMainWindow.MessageNotify(log, type, 5);
                if (type == LogType.Log)
                    Debug.Log(log);
                else
                    Debug.LogWarning(log);

                DrawManager.Instance.RedrawLayer(_assistant.OwnerLayer);
                
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(x);
                }
            };
        }

        void CancelCurrentTask()
        {
            if(_currentTaskCts == null) return;
            if(_currentTaskCts.IsCancellationRequested) return;
            _currentTaskCts.Cancel();
        }

        private void RunTask()
        {
            _currentTaskCts?.Cancel();

            _currentTaskCts = new CancellationTokenSource();
            var token = _currentTaskCts.Token;

            var taskbar = LBSMainWindow.Instance.rootVisualElement.Q<ToolBarMain>();
            
            taskbar.OnProgressCancelled -= CancelCurrentTask;
            taskbar.OnProgressCancelled += CancelCurrentTask;

            void ReportProgress(float normalized)
            {
                // Use update so progress applies immediately
                EditorApplication.update += UpdateOnce;

                void UpdateOnce()
                {
                    taskbar.SetProgressPercent(normalized);
                    EditorApplication.update -= UpdateOnce;
                }
            }
            
            taskbar.EnableProcess(true, _assistant.Name);
            Task.Run(() =>
            {
                try
                {
                    _assistant.TryExecute(out string log, out LogType type, 5, ReportProgress, token);
                    EditorApplication.delayCall += () =>
                    {
                        OnExecutionEnd(log, type);
                        taskbar.EnableProcess(false);
                   
                    };

                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WFCAssistant] Task failed: {ex}");
                    EditorApplication.delayCall += () => taskbar.EnableProcess(false);
                }
            }, token);
        }
    }
}