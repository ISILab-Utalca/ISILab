using ISILab.Commons.Utility;
using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor;
using LBS.Components;
using LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class LBSLocalBehaviours : LBSInspector
    {
        private LBSLayer _target;

        #region CONSTRUCTORS
        public LBSLocalBehaviours()
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSLocalBehaviours");
            visualTree.CloneTree(this);

            noContentPanel = this.Q<VisualElement>("NoContentPanel");
            contentPanel = this.Q<VisualElement>("ContentBehaviour");

            this.Q<Button>("Add").SetEnabled(false);
            this.Q<Button>("Add").SetDisplay(false);
        }
        #endregion


        #region METHODS
        public override void InitCustomEditors(ref List<LBSLayer> layers)
        {
            foreach (LBSLayer _refLayer in layers)
            {
               // var layer = reflayer.Clone() as LBSLayer;
                if (_refLayer == null) continue;
                foreach (LBSBehaviour behaviour in _refLayer.Behaviours)
                {
                    if (behaviour is NoteBehaviour)
                        continue;

                    Assert.IsNotNull(behaviour,  "Behaviour is null");
                    Type type = behaviour.GetType();

                    if (customEditor.ContainsKey(type)) continue;

                    var ves = Reflection.GetClassesWith<LBSCustomEditorAttribute>()
                        .Where(t => t.Item2.Any(v => v.type == type)).ToList();

                    if (!ves.Any())
                    {
                        Debug.LogWarning("[ISI Lab] No class marked as LBSCustomEditor found for type: " + type);
                        continue;
                    }

                    Type behaviourEditorType = ves.First().Item1;
                    if (behaviourEditorType == null) continue;
                    customEditor.Add(type, ves.First());
                }
            }
        }

        public override void SetTarget(LBSLayer layer)
        {
            Stopwatch sw = Stopwatch.StartNew();
            long last = sw.ElapsedMilliseconds;

            void Log(string name)
            {
                long now = sw.ElapsedMilliseconds;
                Debug.Log($"{name}: {now - last} ms");
                last = now;
            }

            noContentPanel.SetDisplay(layer is null);
            contentPanel.Clear();
            _target = layer;
            
            if (layer == null)
                return;
            
            noContentPanel.SetDisplay(!_target.Behaviours.Any());

            OnFocus = null;
            OnUnfocus = null;

            // Add the tools into the toolkit and set the data of behaviour
            foreach (var behaviour in _target.Behaviours)
            {
                if (behaviour is NoteBehaviour)
                    continue;

                Type editorType = customEditor.GetValueOrDefault(behaviour.GetType()).Item1;
                if(editorType == null) continue;

                LBSCustomEditor instance = null;
              //  Log("pre setinfo instance EDITOR");
                if (editorInstances.TryGetValue(behaviour.GetType(), out var editor) && editor is LBSCustomEditor existingEditor)
                {
                    instance = existingEditor;
                }
                else
                {
                    instance = Activator.CreateInstance(editorType, behaviour) as LBSCustomEditor;
                }

              //  Log("pre setinfo");
                instance?.SetInfo(behaviour);
                Log("Setinfo");
                ToolKit.Instance.SetTarget(instance);
                Log("postTool");
                OnFocus += instance.OnFocus;
                OnUnfocus += instance.OnUnfocus;

                var content = new InspectorContentPanel(instance, behaviour.Name, behaviour.Icon, behaviour.ColorTint);
                contentPanel.Add(content);

                editorInstances.TryAdd(behaviour.GetType(), instance);
            }
        }

        public override void Repaint()
        {
            if(_target is not null)SetTarget(_target);
            MarkDirtyRepaint();
        }
        #endregion
    }
}