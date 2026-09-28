using UnityEngine.UIElements;

namespace ISILab.LBS.Editor
{
    /// <summary>
    /// Base for any class intended to display a menu content, configuration interface or sub-element within a window or inspector.<br/>
    /// Derived classes should use the <see cref="LBSCustomEditorAttribute"/>.
    /// </summary>
    public abstract class LBSCustomEditor : VisualElement
    {
        #region FIELDS
        protected object target;
        #endregion

        /// <summary>
        /// The instance intended to visually represent.
        /// </summary>
        public object Target => target;

        #region CONSTRUCTORS
        public LBSCustomEditor() { }

        public LBSCustomEditor(object target)
        {
            this.target = target;
        }
        #endregion

        #region METHODS
        public virtual void ContextMenu(ContextualMenuPopulateEvent evt) { }

        public virtual void Repaint() { }

        /// <summary>
        /// Callback invoked when the editor is focused in the inspector.
        /// </summary>
        public virtual void OnFocus() { }
        
        /// <summary>
        /// Callback invoked when focus is lost.
        /// </summary>
        public virtual void OnUnfocus() { }

        /// <summary>
        /// Sets fundamental data and prepares the editor to be displayed.
        /// </summary>
        /// <param name="paramTarget">Instance to visually represent.</param>
        public abstract void SetInfo(object paramTarget);

        /// <summary>
        /// Responsible for loading or creating the visual elements to be displayed, as well as for initializing and configuring their callbacks.
        /// </summary>
        /// <returns></returns>
        protected abstract VisualElement CreateVisualElement();

        #endregion

    }
}