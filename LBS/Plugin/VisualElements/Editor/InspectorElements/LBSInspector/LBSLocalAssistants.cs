using ISILab.Commons.Utility;
using ISILab.Commons.Utility.Editor;
using LBS.Components;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Extensions;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Editor;
using LBS.VisualElements;
using ISILab.LBS.Assistants;
using ISILab.LBS.Plugin.Components.Behaviours;
using UnityEngine.Assertions;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class LBSLocalAssistants : LBSInspector
    {

        private LBSLayer _target;
        
        #region CONSTRUCTORS
        public LBSLocalAssistants()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSLocalAssistants");
            visualTree.CloneTree(this);
            
            noContentPanel = this.Q<VisualElement>("NoContentPanel");
            contentPanel = this.Q<VisualElement>("ContentAssist");
            
            this.Q<Button>("Add").SetEnabled(false);
        }
        #endregion
        
        #region METHODS
        public override void InitCustomEditors(ref List<LBSLayer> layers)
        {
            foreach (LBSLayer _reflayer in layers)
            {
                LBSLayer layer = (LBSLayer)_reflayer.Clone();
                Assert.IsNotNull(layer, "Layer could not be created");
                if (layer == null) continue;
                foreach (var assistant in layer.Assistants)
                {
                    var type = assistant.GetType();
                    if (customEditor.ContainsKey(type)) continue;
                    var ves = Reflection.GetClassesWith<LBSCustomEditorAttribute>()
                        .Where(t => t.Item2.Any(v => v.type == type)).ToList();

                    if (!ves.Any())
                    {
                        Debug.LogWarning("[ISI Lab] No class marked as LBSCustomEditor found for type: " + type);
                        continue;
                    }

                    Type assistantEditorType = ves.First().Item1;
                    if (assistantEditorType == null) continue;
                    customEditor.Add(type, assistantEditorType);
                
                }
            }
        }

        public override void SetTarget(LBSLayer layer)
        {
            noContentPanel.SetDisplay(layer is null);
            contentPanel.Clear();
            _target = layer;
            
            if (layer == null)
                return;
            
            noContentPanel.SetDisplay(!_target.Assistants.Any());

            OnFocus = null;
            OnUnfocus = null;

            LBSAssistant currentAssistant = null;

            // Add the tools into the toolkit and set the data of behaviour
            foreach (LBSAssistant assistant in _target.Assistants)
            {
                currentAssistant = assistant;

                Type editorType = customEditor.GetValueOrDefault(assistant.GetType());
                if(editorType == null) continue;
                
                LBSCustomEditor instance = Activator.CreateInstance(editorType, assistant) as LBSCustomEditor;
              
                instance.SetInfo(assistant);
                ToolKit.Instance.SetTarget(instance);

                OnFocus += instance.OnFocus;
                OnUnfocus += instance.OnUnfocus;
                
                var content = new InspectorContentPanel(instance, assistant.Name, assistant.Icon, assistant.ColorTint);
                contentPanel.Add(content);
                currentAssistant.OnTermination += OnTerminationBaseCallback;
                
                if (activeEditor is null) continue;
                InspectorInstance entry = new InspectorInstance(assistant.GetType(), _target);
                activeEditor[entry] = instance;
            }

            return; /// END OF METHOD

            // Local functions

            void OnTerminationBaseCallback(string log, LogType type)
            {
                if(currentAssistant is null) return;
                currentAssistant.OnTermination -= OnTerminationBaseCallback;
                LBSInspectorPanel.Instance.SetTarget(currentAssistant.OwnerLayer);
                Debug.Log("OnTermination");
            }
        }
        
        

        public override void Repaint()
        {
            if(_target is not null) SetTarget(_target);
            MarkDirtyRepaint();
        }
        #endregion
    }
    
}