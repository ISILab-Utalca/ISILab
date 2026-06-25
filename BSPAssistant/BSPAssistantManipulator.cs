using ISILab.LBS;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar;
using ISILab.LBS.Plugin.VisualElements.Editor.AssistantThreads;
using LBS.Components;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab
{
    public class BSPAssistantManipulator : ManipulateTeselation, IAssistantThreadedEditor
    {
        // Fields
        private Vector2Int _cornerStart;
        private BSPAssistant assistant;

        // Inherited Properties
        protected override string IconGuid => "5ab039ea1b079eb4dbe013d7a618c2aa";
        public CancellationToken CancelToken { set; get; }
        public CancellationTokenSource CancellationTokenSource { set; get; }
        public ToolBarMain TaskBar { set; get; }

        // Constructor
        public BSPAssistantManipulator()
        {
            Feedback.fixToTeselation = true;
            Name = "BSP Dungeon Generator";
            Description = "Select an area to generate a dungeon usign the Binary Space Partition algorithm.";
        }

        // Manipulator Methods
        public override void Init(LBSLayer layer, object owner)
        {
            base.Init(layer, owner);
            assistant = owner as BSPAssistant;
        }
        protected override void OnMouseDown(VisualElement element, Vector2Int position, MouseDownEvent e)
        {
            _cornerStart = position;
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            base.OnMouseUp(element, endPosition, e);

            //If esc key was pressed, cancel the operation
            if (ForceCancel)
            {
                ForceCancel = false;
                return;
            }

            var corners = assistant.OwnerLayer.ToFixedPosition(_cornerStart, endPosition);
            var mapWidth = corners.max.x - corners.min.x;
            var mapHeight = corners.max.y - corners.min.y;
            assistant.Area = new RectInt(corners.min.x, corners.min.y, mapWidth, mapHeight);
            Execute();
        }

        private void Execute()
        {
            string insideStyle = assistant.Schema.PressetInsideStyle.name;
            string outsideStyle = assistant.Schema.PressetOutsideStyle.name;
            // Save history version to revert if necessary
            LoadedLevel x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "Execute BSPDungeon");

            ((IAssistantThreadedEditor)this).SetUpTask(this, assistant);
            Task.Run(() =>
            {
                try
                {
                    assistant.RunAsync(insideStyle, outsideStyle,
                        ((IAssistantThreadedEditor)this).ReportProgress, CancelToken);
                    EditorApplication.delayCall += () => assistant.OnTermination.Invoke("BSPDungeon Generated", LogType.Log, LBSController.CurrentLevel);
                }
                catch (System.Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, assistant);
                    Debug.LogError("[BSPDungeonAssistantEditor]: " + ex.Message);
                }
            }, CancelToken);
        }

        void IAssistantThreadedEditor.OnAssistantTermination(string log, LogType type, Object loadedLevel)
        {
            LBSMainWindow.MessageNotify(new LBSLog(log, type));

            // Mark as dirty
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(loadedLevel);
            }

            assistant.Schema.RecalculateWalls();
            DrawManager.Instance.RedrawLevel(LBS.LBS.loadedLevel.data);
            LBSMainWindow.Instance.layerPanel.SetSelectedLayer(assistant.Schema.OwnerLayer);

            TaskBar.EnableProcess(false);
            assistant.OnTermination = null;
        }
    }
}
