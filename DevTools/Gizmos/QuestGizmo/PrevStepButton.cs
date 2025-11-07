using ISILab.Commons.Utility.Editor;
using System;
using System.Collections.Generic;
using ISI_Lab.LBS.DevTools;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ISI_Lab.LBS.Plugin.MapTools.Generators3D;
using ISILab.LBS.Modules;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ISILab.LBS.VisualElements
{
    public class PrevStepButton : GraphElement
    {
        private Button ButtonElement { get; }
        
        public PrevStepButton(Button buttonTemplate, QuestBarView questBarView, QuestTrigger trigger)
        {
            if (buttonTemplate == null)
            {
                Debug.LogWarning("PrevStepButton: Button template is null — cannot create button.");
                return;
            }
            
            // Create a new UI Toolkit button
            ButtonElement = new Button
            {   style =
                {
                    
                    width = buttonTemplate.style.width,
                    height = buttonTemplate.style.height,
                    backgroundColor = buttonTemplate.style.backgroundColor,
                    
                    borderBottomWidth = buttonTemplate.style.borderBottomWidth,
                    borderTopWidth = buttonTemplate.style.borderTopWidth,
                    borderLeftWidth = buttonTemplate.style.borderLeftWidth,
                    borderRightWidth = buttonTemplate.style.borderRightWidth,
                    borderTopColor = buttonTemplate.style.borderTopColor,
                    borderBottomColor = buttonTemplate.style.borderBottomColor,
                    borderLeftColor = buttonTemplate.style.borderLeftColor,
                    borderRightColor = buttonTemplate.style.borderRightColor,
                    
                },
                iconImage = buttonTemplate.iconImage
            };

            // Copy USS style classes
            foreach (string className in buttonTemplate.GetClasses()) ButtonElement.AddToClassList(className);
            
            ButtonElement.clicked += () => questBarView.OnPrevStepClicked(trigger);
            
            // Add button to this GraphElement
            Add(ButtonElement);

            // Optional styling for layout
            style.position = Position.Absolute;
        }
        
    }
}