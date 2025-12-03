using System;
using System.Collections.Generic;
using ISILab.LBS.Editor;
using LBS.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    public struct InspectorInstance : IEquatable<InspectorInstance>
    {
        public Type typeTarget;
        public LBSLayer layer;

        public InspectorInstance(Type typeTarget, LBSLayer layer)
        {
            this.typeTarget = typeTarget;
            this.layer = layer;
        }

        public bool Equals(InspectorInstance other)
        {
            return Equals(typeTarget, other.typeTarget) && Equals(layer, other.layer);
        }

        public override bool Equals(object obj)
        {
            return obj is InspectorInstance other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(typeTarget, layer);
        }
    }
    
    public abstract class LBSInspector : VisualElement
    {
        /// <summary>
        /// Dictionary for behaviour, assistants, it assumes each one only has 1 editor!
        /// </summary>
        protected Dictionary<Type, Tuple<Type, IEnumerable<LBSCustomEditorAttribute>>> customEditor = new();

        protected Dictionary<InspectorInstance, VisualElement> activeEditor = new();
        
        protected VisualElement noContentPanel;
        protected VisualElement contentPanel;

        public Action OnFocus;
        public Action OnUnfocus;
        
        /// <summary>
        /// Gets the classes of editors per component, no avoid using reflection on each instance creation
        /// </summary>
        /// <param name="layer"></param>
        public abstract void InitCustomEditors(ref List<LBSLayer> layers);
        /// <summary>
        /// Sets the active layer into the panel to update the different components of a layer, such as modules,
        /// behaviours, assistants and toolkit. 
        /// </summary>
        /// <param name="layer"></param
        
        public abstract void SetTarget(LBSLayer layer);
        /// <summary>
        /// Markes the panel as dirty and calls resetTarget
        /// <param name="layer"></param>
        public virtual void Repaint() 
        {
            Debug.LogWarning("[ISILab]: The inspector (" + ToString() + ") does not implement repainting.");
        }
        
        public VisualElement GetInspector(Type ObjectType, LBSLayer Layer)
        {
            foreach (KeyValuePair<InspectorInstance, VisualElement> entry in activeEditor)
            {
                if(entry.Key.typeTarget != ObjectType) continue;
                if(!Equals(entry.Key.layer, Layer)) continue;
                return entry.Value;
            }
            
            return null;
        }
    }
}