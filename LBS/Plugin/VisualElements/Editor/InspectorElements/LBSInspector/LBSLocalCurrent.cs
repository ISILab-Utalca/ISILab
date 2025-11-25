using ISILab.Commons.Utility;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Editor;
using LBS.Components;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Extensions;
using ISILab.LBS.Modules;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class LBSLocalCurrent : LBSInspector
    {
        private LBSLayer _target;
        
        private readonly ModulesPanel modulesPanel;
        private readonly LayerInfoView layerInfoView;

        #region CONSTRUCTORS
        public LBSLocalCurrent()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSLocalCurrent");
            visualTree.CloneTree(this);

            contentPanel = this.Q<VisualElement>("SelectedContent");

            modulesPanel = this.Q<ModulesPanel>();
            layerInfoView = this.Q<LayerInfoView>();
        }
        #endregion
        
        #region METHODS
        public override void InitCustomEditors(ref List<LBSLayer> _layers)
        {
            foreach (LBSLayer refLayer in _layers)
            {
                if (refLayer == null) continue;
                foreach (LBSModule module in refLayer.Modules)
                {
                    Type type = module.GetType();
                    var ves = Reflection.GetClassesWith<LBSCustomEditorAttribute>()
                        .Where(t => t.Item2.Any(v => v.type == type)).ToList();

                    if (!ves.Any())
                    {
                     //   Debug.LogWarning("[ISI Lab] No class marked as LBSCustomEditor found for type: " + type);
                        continue;
                    }

                    Type moduleEditorType = ves.First().Item1;
                    if (moduleEditorType == null) continue;
                    customEditor.Add(type, moduleEditorType);
                }
            }
        }

        public override void SetTarget(LBSLayer layer)
        {
            noContentPanel.SetDisplay(layer is null);
            contentPanel.Clear();
            _target = layer;

            if (layer == null)
            {
                layerInfoView.SetInfo(null); // no layer, hide info
                modulesPanel.SetInfo(new List<LBSModule>()); //pass an empty list
                return;    
            }
            
            noContentPanel.SetDisplay(!_target.Modules.Any());
            
            ToolKit.Instance.InitGeneralTools(_target);
            
            modulesPanel.SetInfo(_target.Modules);
            layerInfoView.SetInfo(_target);
            
            
        }

        public override void Repaint()
        {
            if(_target is not null) SetTarget(_target);
            MarkDirtyRepaint();
        }

        public void SetSelectedVe(List<object> objs)
        {
            contentPanel.Clear();

            foreach (var obj in objs)
            {
                // Check if obj is valid
                if (obj == null)
                {
                    contentPanel.Add(new Label("[NULL]"));
                    continue;
                }

                // Get type of element
                var type = obj.GetType();

                // Get the editors of the selectable elements
                var ves = Reflection.GetClassesWith<LBSCustomEditorAttribute>()
                        .Where(t => t.Item2.Any(v => v.type == type)).ToList();

                if (ves.Count <= 0)
                {
                    // Add basic label if no have specific editor
                    contentPanel.Add(new Label("'" + type + "' does not contain a visualization."));
                    continue;
                }

                // Get editor class
                var editorType = ves.First().Item1;
                
                // set target info on visual element
                if (Activator.CreateInstance(editorType) is not LBSCustomEditor instance) continue;
                
                instance.SetInfo(obj);
                ToolKit.Instance.SetTarget(instance);

                // create content container
                var container = new DataContent(instance, ves.First().Item2.First().name);
                contentPanel.Add(container);
                
                if (activeEditor is null) continue;
                InspectorInstance entry = new InspectorInstance(obj.GetType(), _target);
                activeEditor[entry] = instance;
            }
        }
        #endregion
    }
}