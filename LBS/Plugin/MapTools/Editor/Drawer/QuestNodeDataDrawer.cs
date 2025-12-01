using System;
using System.Linq;
using ISILab.LBS.VisualElements.Editor;
using UnityEngine;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Modules;
using ISILab.LBS.VisualElements;
using LBS.Components;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace ISILab.LBS.Drawers.Editor
{
    [Drawer(typeof(QuestNodeBehaviour))]
    public class QuestNodeBehaviourDrawer : Drawer
    {
     
        private Action _onChangeAction;
        
        /// <summary>
        /// Draws the information that corresponds to the quest node behavior selected node.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="view"></param>
        /// <param name="tesselationSize"></param>
        public override void Draw(object target, MainView view, Vector2 tesselationSize)
        {
            if (target is not QuestNodeBehaviour behaviour) return;
            if (behaviour.OwnerLayer is not { } layer) return;
            view.ClearLayerContainer(behaviour.OwnerLayer, true);
            
            if (_onChangeAction != null) layer.OnChange -= _onChangeAction;
            _onChangeAction = ClearElements(view, layer, behaviour);
            layer.OnChange += _onChangeAction;
            
            if (!Equals(LBSMainWindow.Instance._selectedLayer, layer)) return; 
            
            QuestActionData actionData = behaviour.Graph.GetNodeData();
            if (actionData is null) return;

            
            // Selected Node Trigger View 
            actionData.Resize();
            
            //TODO: Use the new drawing system... maybe?
            
           // var nt = behaviour.RetrieveNewTiles();
           // if (nt == null || !nt.Any()) return;
           // temp fix just clearing the whole layer, as this is called BEFORE the other drawer this one clears it once

            DisplayStyle display = (DisplayStyle)(behaviour.OwnerLayer.IsVisible ? 0 : 1);

            QuestGraph graph = behaviour.OwnerLayer.GetModule<QuestGraph>();
            if(graph is null) return;
            if(graph.SelectedGraphNode is null) return;

            QuestActionView selectedActionView = null;
            foreach (GraphElement graphElement in view.GetAllElementsInLayer(behaviour.OwnerLayer))
            {
                if (graphElement is not QuestActionView qav) continue;
                if (qav.Node.Equals(graph.SelectedGraphNode))
                {
                    selectedActionView = qav;
                }
            }
            
            #region BundleGraph View
            
            // Trigger Position
            TriggerElementArea triggerBase = new(actionData,actionData.Area, selectedActionView?.OnMoving)
            {
                style =
                {
                    display = display
                }
            };

            // Stores using the behavior as key
            view.AddElementToLayerContainer(behaviour.OwnerLayer, behaviour, triggerBase);
            
            switch (actionData)
            {
                case DataKill dataKill:
                    if(!dataKill.bundlesToKill.Any()) break;
                    foreach (BundleGraph bundle in dataKill.bundlesToKill)
                    {
                        if (bundle is null || !bundle.Valid()) continue;
                        
                        TriggerElementArea visual = new(actionData, bundle.Area, selectedActionView?.OnMoving)
                        {
                            style =
                            {
                                display = display
                            }
                        };
                        view.AddElementToLayerContainer(behaviour.OwnerLayer, behaviour, visual);
                    }
                    break;
                
                case DataStealth dataStealth:
                    if(dataStealth.bundlesObservers == null || !dataStealth.bundlesObservers.Any()) break;
                    foreach (BundleGraph bundle in dataStealth.bundlesObservers)
                    {
                        if (bundle is null || !bundle.Valid()) continue;
                        
                        TriggerElementArea visual = new(actionData, bundle.Area, selectedActionView?.OnMoving)
                        {
                            style =
                            {
                                display = display
                            }
                        };

                        view.AddElementToLayerContainer(behaviour.OwnerLayer, behaviour, visual);
                    }
                    break;
                
                case DataTake dataTake:
                    if (dataTake.bundleToTake.Valid())
                    {
                        TriggerElementArea visual = new(actionData, dataTake.bundleToTake.Area, selectedActionView?.OnMoving)
                        {
                            style =
                            {
                                display = display
                            }
                        };
                        view.AddElementToLayerContainer(behaviour.OwnerLayer, behaviour, visual);
                    }
                    break;
                
                case DataRead dataRead:
                    if (dataRead.bundleToRead.Valid())
                    {
                        TriggerElementArea visual = new(actionData, dataRead.bundleToRead.Area, selectedActionView?.OnMoving)
                        {
                            style =
                            {
                                display = display
                            }
                        };
                        view.AddElementToLayerContainer(behaviour.OwnerLayer, behaviour, visual);
                    }
                    break;
                
                case DataGive dataGive:
                    if (dataGive.bundleGiveTo.Valid())
                    {
                        TriggerElementArea visual = new(actionData, dataGive.bundleGiveTo.Area, selectedActionView?.OnMoving)
                        {
                            style =
                            {
                                display = display
                            }
                        };
                        view.AddElementToLayerContainer(behaviour.OwnerLayer, behaviour, visual);
                    }
                    break;
                
                case DataReport dataReport:
                    if (dataReport.bundleReportTo.Valid())
                    {
                        TriggerElementArea visual = new(actionData, dataReport.bundleReportTo.Area, selectedActionView?.OnMoving)
                        {
                            style =
                            {
                                display = display
                            }
                        };
                        view.AddElementToLayerContainer(behaviour.OwnerLayer, behaviour, visual);
                    }
                    break;
                
                case DataSpy dataSpy:
                    if (dataSpy.bundleToSpy.Valid())
                    {
                        TriggerElementArea visual = new(actionData, dataSpy.bundleToSpy.Area, selectedActionView?.OnMoving)
                        {
                            style =
                            {
                                display = display
                            }
                        };
                        view.AddElementToLayerContainer(behaviour.OwnerLayer, behaviour, visual);
                    }
                    break;
                
                case DataListen dataListen:
                    if (dataListen.bundleListenTo.Valid())
                    {
                        TriggerElementArea visual = new(actionData, dataListen.bundleListenTo.Area, selectedActionView?.OnMoving)
                        {
                            style =
                            {
                                display = display
                            }
                        };
                        view.AddElementToLayerContainer(behaviour.OwnerLayer, behaviour, visual);
                    }
                    break;
            }
            
            #endregion
            
  
        }

        private static Action ClearElements(MainView view, LBSLayer layer, QuestNodeBehaviour behaviour)
        {
            return () =>
            {
                view.ClearLayerComponentView(layer, behaviour);
            };
        }

        public override void ShowVisuals(object target, MainView view)
        {
            // Get behaviours
            if (target is not QuestNodeBehaviour behaviour) return;
            
            foreach (object tile in behaviour.Keys)
            {
                foreach (GraphElement graphElement in view.GetElementsFromLayerContainer(behaviour.OwnerLayer, tile).Where(graphElement => graphElement != null))
                {
                    graphElement.style.display = DisplayStyle.Flex;
                }
            }
        }
        public override void HideVisuals(object target, MainView view)
        {
            // Get behaviours
            if (target is not QuestNodeBehaviour behaviour) return;
            
            foreach (object tile in behaviour.Keys)
            {
                if (tile == null) continue;

                var elements = view.GetElementsFromLayerContainer(behaviour.OwnerLayer, tile);
                foreach (GraphElement graphElement in elements)
                {
                    graphElement.style.display = DisplayStyle.None;
                }
            }
        }
    }        
    
}