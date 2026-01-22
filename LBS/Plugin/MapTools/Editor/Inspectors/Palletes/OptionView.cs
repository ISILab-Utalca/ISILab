using ISILab.Commons.Utility.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Behaviours.Editor;
using ISILab.LBS.VisualElements;
using UnityEditor.UIElements;


namespace LBS.VisualElements
{
    
    [UxmlElement]
    public partial class OptionView : VisualElement
    {
        //private Color selected = new Color(1,1,1,0.1f);
        //private Color nonSelected = new Color(1, 1, 1, 0f);

        private Label label;
        private Button button;
        private VisualElement icon;
        private VisualElement border;
        private ToolbarMenu _toolbar;
        
        public object target;

        public Action<object> OnSelect;
        private Action<OptionView, object> OnSetView;

        #region PROPERTIES
        public object Target
        {
            get => target;
            set
            {
                target = value;
                OnSetView?.Invoke(this, target);
            }
        }

        public string Label
        {
            set => label.text = value;
        }

        public VectorImage Icon
        {
            set => icon.style.backgroundImage = new StyleBackground(value);
        }

        
        [UxmlAttribute]
        public Color Color
        {
            set
            {
                if (button != null) button.style.backgroundColor = value;
            }
        }
        #endregion


        public OptionView() : base()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("OptionView");
            visualTree.CloneTree(this);
            // Init View
            this.label = this.Q<Label>("Label");
            this.icon = this.Q<VisualElement>("Icon");
            this.border = this.Q<VisualElement>("Border");
            //border.SetBorder(border.style.backgroundColor.value, 2);
            this.button = this.Q<Button>();
        }
        public OptionView(object target, Action<object> onSelect, Action<object> onRemove, Action<OptionView, object> onSetView): this()
        {
      
            button.clicked += () => { 
                this.OnSelect?.Invoke(target);
                SetSelected(true);
            };
            _toolbar = this.Q<ToolbarMenu>("ToolBar");
            if(_toolbar != null)
            {
                _toolbar.menu.AppendAction("Delete Zone", action =>
                {
                    DeleteZone(action, onRemove);
                });
                _toolbar.style.display = DisplayStyle.None;
            }
            
            // Init Fields
            this.target = target;

            // Init Events
            this.OnSelect = onSelect;

            this.OnSetView = onSetView;
            OnSetView?.Invoke(this, target);
            
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            /* Commen so only the button triggers - currently there can be text so instead the 
                whole visual element triggers
            */

            if (evt.button == 0)
            {
                this.OnSelect?.Invoke(target);
                SetSelected(true);
            }
            ///

            else if (evt.button == 1 && _toolbar != null)
            {
                _toolbar.style.display = DisplayStyle.Flex;
                _toolbar.ShowMenu();
            }
            
        }

        private void DeleteZone(DropdownMenuAction obj, Action<object> Remove)
        {
            Remove.Invoke(target);
        }

        public void SetSelected(bool value)
        {
            if(value)
            {
                //border.SetBorder(selected, 2);
                //border.style.backgroundColor = selected;
                AddToClassList("prop-state--checked");
            }
            else
            {
                //border.SetBorder(nonSelected, 2);
                //border.style.backgroundColor = nonSelected;
                RemoveFromClassList("prop-state--checked");
            }
        }
    }
}