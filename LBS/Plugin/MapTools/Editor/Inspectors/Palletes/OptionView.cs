using ISILab.Commons.Utility.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Behaviours.Editor;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
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
        //private Button button;
        private VisualElement frame;
        private VisualElement iconVisualElement;
        private VisualElement border;
        private ToolbarMenu _toolbar;
        
        // Manipulator
        private Clickable clickableManipulator;
        
        // Parameters backing fields
        private VectorImage icon;
        private Color frameColor = Color.black;
        
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

        
        [UxmlAttribute]
        public string Label
        {
            get => label.text;
            set => label.text = value;
        }

        [UxmlAttribute]
        public VectorImage Icon
        {
            get => icon;
            set
            {
                icon = value;
                if (iconVisualElement != null)
                {
                    if (icon != null)
                    {
                        iconVisualElement.style.backgroundImage = new StyleBackground(value);
                    }
                    else
                    {
                        iconVisualElement.style.backgroundImage = new StyleBackground(LBSAssetMacro.LoadPlaceholderVectorImage());
                    }
                }
            }
        }


        [UxmlAttribute]
        public Color FrameColor
        {
            get => frameColor;
            set
            {
                frameColor = value;
                if (frame != null)
                {
                    frame.style.backgroundColor = value;
                }
            }
        }
        #endregion


        public OptionView() : base()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("OptionView");
            visualTree.CloneTree(this);
            // Init View
            this.AddToClassList("lbs-item");
            this.style.height = 84;
            
            this.label = this.Q<Label>("Label");
            this.iconVisualElement = this.Q<VisualElement>("Icon");
            this.border = this.Q<VisualElement>("Border");
            //border.SetBorder(border.style.backgroundColor.value, 2);
            //this.button = this.Q<Button>();

            clickableManipulator = new Clickable(() =>
            {
                SetSelected(true);
            });
            this.AddManipulator(clickableManipulator);
            this.pickingMode = PickingMode.Position;
            this.focusable = true;
            this.style.overflow = Overflow.Hidden;

        }
        public OptionView(object target, Action<object> onSelect, Action<object> onRemove, Action<OptionView, object> onSetView): this()
        {
      
            clickableManipulator.clicked += () => { 
                this.OnSelect?.Invoke(target);
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

                AddToClassList("prop-state--checked");
            }
            else
            {
                RemoveFromClassList("prop-state--checked");
            }
        }
    }
}