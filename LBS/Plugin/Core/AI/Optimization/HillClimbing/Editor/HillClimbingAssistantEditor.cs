using System;
using System.Threading;
using System.Threading.Tasks;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Editor.Windows;

using LBS;
using LBS.Components;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.VisualElements.Editor.AssistantThreads;
using ISILab.LBS.VisualElements.Editor;
using LBS.VisualElements;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ToolBarMain = ISILab.LBS.Plugin.UI.Editor.Windows.ToolBar.ToolBarMain;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("HillClimbingAssistant", typeof(HillClimbingAssistant))]
    public class HillClimbingAssistantEditor : LBSCustomEditor, IToolProvider, IAssistantThreadedEditor
    {
        #region FIELDS
        private readonly UnityEngine.Color AssistantColor = LBSSettings.Instance.view.assistantColor;

        private HillClimbingAssistant _assistant;

        private Foldout foldout;
        private Button revert;
        private Button execute;
        private Button executeOne;
        
        private LBSCustomToggleField toggle;
        private LBSCustomToggleField benchmarkToggle;
        private Toggle toggleTimer;

        private Button recalculate;

        private LBSLayer tempLayer;

        // Manipulators
        private SetZoneConnection setZoneConnection;
        private RemoveZoneConnection removeZoneConnection;

        #endregion
        
        public HillClimbingAssistantEditor(object target) : base(target)
        {
            _assistant = target as HillClimbingAssistant;

            CreateVisualElement();

            var window = EditorWindow.GetWindow<LBSMainWindow>();

            // NO longer redrawing the whole window we aint psychopaths!
          //  hillClimbing.OnTermination += window.Repaint;
        }

        public override void Repaint()
        {
            var moduleConstr = _assistant.OwnerLayer.GetModule<ConstrainsZonesModule>();
            foldout.Clear();
            foreach (var constraint in moduleConstr.Constraints)
            {
                var view = new ConstraintView();
                view.SetData(constraint);
                foldout.Add(view);
            }
        }

        public override void SetInfo(object paramTarget)
        {
            _assistant = paramTarget as HillClimbingAssistant;
        }

        public void SetTools(ToolKit toolKit)
        {
            setZoneConnection = new SetZoneConnection();
            var t1 = new LBSTool(setZoneConnection);
            t1.OnSelect += LBSInspectorPanel.ActivateAssistantTab;
            t1.Init(_assistant.OwnerLayer, _assistant);
            
            removeZoneConnection = new RemoveZoneConnection();
            var t2 = new LBSTool(removeZoneConnection);
            t2.OnSelect += LBSInspectorPanel.ActivateAssistantTab;
            
            setZoneConnection.SetRemover(removeZoneConnection);

            toolKit.ActivateTool(t1, _assistant.OwnerLayer, _assistant);
            toolKit.ActivateTool(t2, _assistant.OwnerLayer, _assistant);
        }

        protected override VisualElement CreateVisualElement()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("HillClimbingEditor");
            visualTree.CloneTree(this);

            var moduleConstr = _assistant.OwnerLayer.GetModule<ConstrainsZonesModule>();

            // Foldout
            foldout = this.Q<LBSCustomFoldout>();
            foreach (var constraint in moduleConstr.Constraints)
            {
                var view = new ConstraintView();
                view.SetData(constraint);
                foldout.Add(view);
            }

            // Print Timers
            toggleTimer = this.Q<Toggle>("ShowTimerToggle");
            toggleTimer.RegisterCallback<ChangeEvent<bool>>(x =>
            {
                _assistant.printClocks = x.newValue;
            });

            // Execute
            execute = this.Q<Button>("Execute");
            execute.clicked += Execute;

            // Execute 1 step
            executeOne = this.Q<Button>("ExecuteOneStep");
            executeOne.clicked += ExecuteOneStep;

            // Show Constraint
            toggle = this.Q<LBSCustomToggleField>("ShowConstraintToggle");
            toggle.value = _assistant.visibleConstraints;
            toggle.RegisterCallback<ChangeEvent<bool>>(x =>
            {
                _assistant.visibleConstraints = x.newValue;
                DrawManager.ReDraw();
            });
            
            benchmarkToggle = this.Q<LBSCustomToggleField>("UseBenchmark");

            recalculate = new Button
            {
                text = "Recalculate Constraints"
            };
            recalculate.clicked += ClickedRecalculate;

            Add(recalculate);

            return this;
        }

        #region IAssistantThreadedEditor
        public CancellationToken CancelToken { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ToolBarMain TaskBar { get; set; }

        void IAssistantThreadedEditor.OnAssistantTermination(string log, LogType type)
        {
            LoadedLevel loadedLevel = LBSController.CurrentLevel;
            LBSMainWindow.MessageNotify(new LBSLog(log, type));

            // Mark as dirty
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(loadedLevel);
            }

            DrawManager.Instance.RedrawLayer(_assistant.OwnerLayer);
            Paint();
                        
            TaskBar.EnableProcess(false);
            _assistant.OnTermination = null;
        }
        #endregion}
        
        private void ClickedRecalculate()
        {
            // Save history version to revert if necessary
            LoadedLevel loadedLevel = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(loadedLevel, "Recalculate Constraints");

            // Recalculate constraints
            RunRecalculateTask();
        }

        private void RunRecalculateTask()
        {
            ((IAssistantThreadedEditor)this).SetUpTask(this, _assistant);
            Task.Run(() =>
            {
                try
                {
                    _assistant.RecalculateConstraint(((IAssistantThreadedEditor)this).ReportProgress, CancelToken);
                    EditorApplication.delayCall += () => _assistant.OnTermination.Invoke("Zone Constraints Recalculated", LogType.Log);
                }
                catch (Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, _assistant);
                }
            }, CancelToken);
        }

        private void Paint()
        {
            Clear();
            CreateVisualElement();
        }

        private void ExecuteOneStep()
        {
            ((IAssistantThreadedEditor)this).SetUpTask(this, _assistant);
            Task.Run(() =>
            {
                try
                {
                    _assistant.ExecuteOneStep(out string log, out LogType type, ((IAssistantThreadedEditor)this).ReportProgress, CancelToken);
                    EditorApplication.delayCall += () => _assistant.OnTermination.Invoke(log, type);
                }
                catch (Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, _assistant);
                }
            }, CancelToken);
        }

        private void Execute()
        {
            // Save history version to revert if necessary
            LoadedLevel x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "Execute HillClimbing");

            ((IAssistantThreadedEditor)this).SetUpTask(this, _assistant);
            Task.Run(() =>
            {
                try
                {
                    _assistant.TryExecute(out string log, out LogType type, ((IAssistantThreadedEditor)this).ReportProgress,
                        CancelToken);
                    EditorApplication.delayCall += () => _assistant.OnTermination.Invoke(log, type);
                }
                catch (Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, _assistant);
                }
            }, CancelToken);
        }
    }
}