using System;
using System.Collections.Generic;
using ISILab.LBS.Editor;
using LBS.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    
    public abstract class LBSInspector : VisualElement
    {
        protected struct EditorKey
        {
            Type type;
            string key;

            public EditorKey(Type type, string key)
            {
                this.type = type;
                this.key = key;
            }
        }

        /// <summary>
        /// Dictionary for behaviour, assistants, it assumes each one only has 1 editor!
        /// </summary>
        protected Dictionary<Type, Tuple<Type, IEnumerable<LBSCustomEditorAttribute>>> customEditor = new();

        protected Dictionary<EditorKey, LBSCustomEditor> editorInstances = new();
        
        protected VisualElement noContentPanel;
        protected VisualElement contentPanel;

        public Action OnFocus;
        public Action OnUnfocus;
        
        /// <summary>
        /// Gets the classes of editors per component, no avoid using reflection on each instance creation
        /// Must be overridden in a derived class to implement custom repainting behavior.
        /// </summary>
        public abstract void InitCustomEditors(ref List<LBSLayer> layers);

        /// <summary>
        /// Sets the active layer into the panel to update the different components of a layer, such as modules,
        /// behaviours, assistants and toolkit. 
        /// </summary>
        /// <param name="layer"></param>
        public abstract void SetTarget(LBSLayer layer);

        /// <summary>
        /// Marks the panel as dirty and requests a repaint. 
        /// Must be overridden in a derived class to implement custom repainting behavior.
        /// </summary>
        public virtual void Repaint() 
        {
            Debug.LogWarning("[ISILab]: The inspector (" + ToString() + ") does not implement repainting.");
        }
        
        public VisualElement GetInspector(Type objectType, string key)
        {
            editorInstances.TryGetValue(new EditorKey(objectType, key), out var editor);            
            return editor;
        }
    }
}