using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS;
using ISILab.LBS.Behaviours;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.Windows.Blueprint;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;

namespace LBS.VisualElements
{
    [UxmlElement]
    public partial class ToolKit : VisualElement
    {
        #region FACTORY

        UxmlColorAttributeDescription m_BaseColor = new UxmlColorAttributeDescription
        {
            name = "base-color",
            defaultValue = new Color(72f / 255f, 72f / 255f, 72f / 255f)
        };

        UxmlColorAttributeDescription m_SelectedColor = new UxmlColorAttributeDescription
        {
            name = "selected-color",
    
            defaultValue = new Color(215f / 255f, 127f / 255f, 45f / 255f)
        };

        UxmlIntAttributeDescription m_Index = new UxmlIntAttributeDescription
        {
            name = "index",
            defaultValue = 0
        };
        #endregion
        
        #region FIELDS
        private Dictionary<Type, (LBSTool, ToolButton)> tools = new();
        
        private (LBSTool, ToolButton) current;
        private bool Initialized;
        private Color baseColor = new(72f / 255f, 72f / 255f, 72f / 255f);
        private int index;
        private int choiceCount;

        private VisualElement content;
        private List<VisualElement> separators = new();

        private ToolButton nextFloorButton;
        private ToolButton prevFloorButton;
        private LBSCustomUnsignedIntegerField floorIndexField;

        #endregion

        #region SINGLETON
        private static ToolKit instance;
        internal static ToolKit Instance 
        {
            get
            {
                return instance;
            }
        }
        #endregion
        
        #region PROPERTIES
        public Color BaseColor
        {
            get => baseColor;
            set => baseColor = value;
        }

        public int Index
        {
            get => index;
            set => index = value;
        }

        public int ChoiceCount
        {
            get => choiceCount;
            set => choiceCount = value;
        }
        #endregion

        #region EVENTS
        public event Action<LBSLayer> OnEndAction;
        public event Action<LBSLayer> OnStartAction;
        #endregion

        #region CONSTRUCTOR
        public ToolKit()
        {
            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("ToolKit");
            visualTree.CloneTree(this);
            content = this.Q<VisualElement>("Content");

            nextFloorButton = this.Q<ToolButton>("NextFloorButton");
            nextFloorButton.style.display = DisplayStyle.None;
            prevFloorButton = this.Q<ToolButton>("PrevFloorButton");
            prevFloorButton.style.display = DisplayStyle.None;
            floorIndexField = this.Q<LBSCustomUnsignedIntegerField>("FloorIndex");
            floorIndexField.style.display = DisplayStyle.None;

            if (!Equals(instance, this))
                instance = this;
        }
        #endregion

        #region METHODS

        public void InitGeneralTools(LBSLayer layer)
        {
            // Manipulators & Tools setup
            (LBSManipulator manipulator, LBSTool tool) CreateTool<T>()
            where T : LBSManipulator, new()
            {
                var manipulator = new T();
                var tool = new LBSTool(manipulator);

                ActivateTool(tool, layer);
                tool.Init(layer, this);

                return (manipulator, tool);
            }

            (LBSManipulator mani, LBSTool tool) select = CreateTool<SelectManipulator>();
            (LBSManipulator mani, LBSTool tool) add = CreateTool<AddNote>();
            (LBSManipulator mani, LBSTool tool) remove = CreateTool<RemoveNote>();
            (LBSManipulator mani, LBSTool tool) capture = CreateTool<CaptureInArea>();
            (LBSManipulator mani, LBSTool tool) print = CreateTool<PrintInArea>();

            add.mani.SetRemover(remove.mani);


            // blueprint set up
            BlueprintPanel bpPanel = LBSMainWindow.Instance.blueprintPanel;
            if (bpPanel is not null)
            {
                bpPanel.Bind(this, capture, print);
            }

            var blueprintVisilibilty = bpPanel.style.display.value;
            bpPanel.CaptureManipulator = (CaptureInArea) capture.mani;
            bpPanel.PrintArea = (PrintInArea)print.mani;
            DisplayManipulator(typeof(CaptureInArea), blueprintVisilibilty);
            DisplayManipulator(typeof(PrintInArea), blueprintVisilibilty);

            capture.tool.OnSelect += () => bpPanel.CaptureManipulator.ClearArea();
            capture.tool.OnDeselect += ()=> bpPanel.CaptureManipulator.ClearArea();
            print.tool.OnSelect += () => bpPanel.PrintArea.ClearPreview();
            print.tool.OnDeselect += () => bpPanel.PrintArea.ClearPreview();


            // Floor buttons set up
            if(layer.FloorCount > 1)
            {
                nextFloorButton.style.display = DisplayStyle.Flex;
                nextFloorButton.RegisterValueChangedCallback(evt =>
                {
                    if (!nextFloorButton.value) return;
                    nextFloorButton.value = false;
                    if (layer is null) return;

                    int newFloor = layer.ActiveFloor + 1;
                    floorIndexField.value = (uint)newFloor;
                    foreach (var l in LBSMainWindow.Instance.GetLayers())
                    {
                        l.ChangeFloor(newFloor);
                        DrawManager.Instance.UpdateLayer(l);
                    }
                });

                prevFloorButton.style.display = DisplayStyle.Flex;
                prevFloorButton.RegisterValueChangedCallback(evt =>
                {
                    if (!prevFloorButton.value) return;
                    prevFloorButton.value = false;
                    if (layer.ActiveFloor - 1 < 0 || layer is null) return;

                    int newFloor = layer.ActiveFloor - 1;
                    floorIndexField.value = (uint)newFloor;
                    foreach (var l in LBSMainWindow.Instance.GetLayers())
                    {
                        l.ChangeFloor(newFloor);
                        DrawManager.Instance.UpdateLayer(l);
                    }
                });

                floorIndexField.style.display = DisplayStyle.Flex;
                floorIndexField.value = (uint)layer.ActiveFloor;
            }
        }

        public void HideFloorButtons()
        {
            nextFloorButton.style.display = DisplayStyle.None;
            prevFloorButton.style.display = DisplayStyle.None;
            floorIndexField.style.display = DisplayStyle.None;
        }


        public object GetActiveManipulator()
        {
            //return content; No idea why this was returning a Visual Element
            return current.Item1?.Manipulator;
        }
        
        public LBSManipulator GetActiveManipulatorInstance()
        {
            if(current.Item1 == null || current.Item2 == null) 
            {
                SetActive(typeof(SelectManipulator)); // should only happen on reloading issues
            }

            return current.Item1?.Manipulator;
        }

        public KeyValuePair<Type, (LBSTool, ToolButton)> GetTool(Type manipulatorType)
        {
            // Find the first matching tool in the dictionary with all null checks
            KeyValuePair<Type, (LBSTool, ToolButton)> foundTool = tools.FirstOrDefault(kvp =>
                kvp is { Key: not null, Value: { Item1: { Manipulator: not null } } } &&
                kvp.Value.Item1.Manipulator.GetType() == manipulatorType);

            return foundTool;
        }

        public VisualElement GetToolButton(Type manipulatorType)
        {
            // Find the first matching tool in the dictionary with all null checks
            return GetTool(manipulatorType).Value.Item2;
        }
        
        public void SetActive(Type manipulatorType)
        {
            //Debug.Log("Manipulator changed to " +  manipulatorType);
            // Ensure manipulatorType is not null
            if (manipulatorType == null)
            {
                //Debug.LogWarning("Manipulator type is null.");
                return;
            }

            // Find the first matching tool in the dictionary
            KeyValuePair<Type, (LBSTool, ToolButton)> foundTool = GetTool(manipulatorType);

            if (foundTool.Key == null)
            {
               // Debug.LogWarning($"Tool of type {manipulatorType.Name} not found.");
                return;
            }
        
            // If another tool was active, blur it
            current.Item2?.OnBlur();
            
            // Set the new current tool and focus it
            current = foundTool.Value;

            foreach (VisualElement btn in content.Children())
            {
                if (btn is ToolButton b)
                    b.SetValueWithoutNotify(false);
            }
            current.Item2.SetValueWithoutNotify(true);

            current.Item2?.OnFocus();

            // Activate its manipulator
            LBSManipulator manipulator = current.Item1.Manipulator;
            MainView.Instance.AddManipulator(manipulator);

            // Notify
            manipulator.OnManipulationNotification += () =>
            {
                LBSMainWindow.Instance.MessageManipulator(manipulator.Description);
            };
            manipulator.OnManipulationNotification?.Invoke();
        }
        
        private void ClearSeparators()
        {
            foreach (VisualElement separator in separators)
            {
                //separator.style.display = DisplayStyle.None;
                separator.parent.Remove(separator);
            }

            separators.Clear();
        }

        public void ActivateTool(LBSTool tool, LBSLayer layer, object provider = null)
        {
            if(tool == null) return;
            
            AddTool(tool);
            tool.Init(layer, provider);

        }

        private void AddTool(LBSTool tool)
        {
            ToolButton button = new ToolButton(tool, content);
            tool.BindButton(button);
            content.Add(button);
            tools[tool.Manipulator.GetType()] = (tool, button);

            button.AddGroupEvent(() => SetActive(tool.Manipulator.GetType()));
            //button.SetColorGroup(LBSSettings.Instance.view.toolkitNormal, LBSSettings.Instance.view.newToolkitSelected); // Not used 
            
            SetUpAdderRemover(tool);

            tool.OnStart += (_) => { OnStartAction?.Invoke(tool.Manipulator.Layer); };
            tool.OnEnd += (_) => { OnEndAction?.Invoke(tool.Manipulator.Layer); };
        }

        private void SetUpAdderRemover(LBSTool tool)
        {
            // For tools that add
            if (tool.Manipulator.Remover != null)
            {
                // Right-clicking removes
                tool.Manipulator.OnManipulationRightClick += () =>
                {
                    // Use GetTool to find the matching Remover tool safely
                    KeyValuePair<Type, (LBSTool, ToolButton)> removerTool = GetTool(tool.Manipulator.Remover.GetType());

                    if (removerTool.Key != null)
                    {
                        // Set the tool as active and update the manipulator
                        SetActive(removerTool.Key);
                        MainView.Instance.SetManipulator(tool.Manipulator.Remover, true);
                    }
                };
            }
            // For tools that remove
            else if (tool.Manipulator.Adder != null)
            {
                // Once it was used via right-click, go back to its corresponding add tool
                tool.Manipulator.OnManipulationRightClickEnd += () =>
                {
                    // Use GetTool to find the matching Adder tool safely
                    KeyValuePair<Type, (LBSTool, ToolButton)> adderTool = GetTool(tool.Manipulator.Adder.GetType());

                    if (adderTool.Key != null)
                    {
                        // Set the tool as active and update the manipulator
                        SetActive(adderTool.Key);
                        MainView.Instance.SetManipulator(tool.Manipulator.Adder, true);
                    }
                };
            }
        }
        
        public void SetTarget(LBSCustomEditor editor)
        {
            if (editor is IToolProvider toolProvider)
            {
                toolProvider.SetTools(this);
            }
        }
        
        public new void Clear()
        {
            if (!tools.Any()) return;
            
            current.Item2?.OnBlur();

            VisualElement father = tools.Values.First().Item2.Father;
            IEnumerable<VisualElement> children = father.Children();
            List<VisualElement> toRemove = new();

            foreach (VisualElement child in children)
            {
                if (child.style.display == DisplayStyle.None)
                {
                    toRemove.Add(child);
                }
                else
                {
                    child.style.display = DisplayStyle.None;
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                father.Remove(toRemove[i]);
            }

            ClearSeparators();
        }
        
        #endregion


        public void SetSeparators()
        {
            ClearSeparators();
            if (tools == null || tools.Count == 0)
                return;
            
            Dictionary<Type, List<ToolButton>> groupedButtons = new();

            foreach ((LBSTool tool, ToolButton button) in tools.Values)
            {
                if (button == null || button.style.display == DisplayStyle.None)
                    continue;

                Type type = tool?.Manipulator?.ObjectType;
                if(type is null) continue;
                
                if (!groupedButtons.ContainsKey(type))
                    groupedButtons[type] = new List<ToolButton>();

                groupedButtons[type].Add(button);
            }

            // presets in desired order!
            List<Type> presentTypes = new()
            {
                typeof(VisualElement),
                typeof(LBSModule),
                typeof(LBSBehaviour),
                typeof(LBSAssistant)
            };
            
            List<ToolButton> lastButtonPerType = new();
            for (int i = 0; i < presentTypes.Count - 1; i++)
            {
                if (groupedButtons.TryGetValue(presentTypes[i], out List<ToolButton> buttons) && buttons.Count > 0)
                {
                    lastButtonPerType.Add(buttons.Last());
                }
            }
            
            foreach (ToolButton button in lastButtonPerType)
            {
                InsertSeparatorAfter(button);
            }
            
            MarkDirtyRepaint();
        }
        
        private void InsertSeparatorAfter(VisualElement element)
        {
            VisualElement separator = new VisualElement();
            separator.style.height = 1;
            separator.style.marginTop = 4;
            separator.style.marginBottom = 4;
            separator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            separator.style.flexGrow = 1;

            VisualElement parent = element.parent;
            if (parent == null) return;

            int index = parent.IndexOf(element);
            if (index >= 0)
            {
                parent.Insert(index + 1, separator);
            }
            
            separators.Add(separator);
        }

        internal void DisplayManipulator(Type manipulatorType, DisplayStyle display)
        {
            foreach ((LBSTool tool, ToolButton button) in tools.Values)
            {
                if (button == null) continue;
                if (tool.Manipulator == null) continue;


                Type type = tool.Manipulator.GetType();
                if (type is null) continue;

                if (manipulatorType == type)
                {
                    button.style.display = display;
                }
            }
            // if the manipulator is not displayed it should be unselected
            if (display == DisplayStyle.None) SetActive(typeof(SelectManipulator));
        }
    }
}