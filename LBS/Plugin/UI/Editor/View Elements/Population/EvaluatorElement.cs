using ISILab.LBS.AI.Categorization;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EvaluatorElement
{
    /// <summary>
    /// Visual element used to show evaluators in a list.
    /// </summary>
    [UxmlElement]
    public partial class EvaluatorElement : LBSComplexVisualElement
    {
        private LBSCustomLabel evLabel;
        private LBSCustomButton evConfigButton;
        private LBSCustomButton evDeleteButton;
        private LBSCustomButton evOpenSOButton;
        private VisualElement interfaceIcon1;
        private VisualElement interfaceIcon2;
        private VisualElement interfaceIcon3;

        public event Action<EvaluatorElement> OnDelete;

        private List<bool> interfaceBoolList;
        private List<VisualElement> interfaceBoolListVisualElements;

        private List<ParameterData> parameters;

        public string EvLabelString
        {
            get => evLabel.text;
            private set => evLabel.text = value;
        }

        public List<bool> InterfaceBoolList
        {
            get => interfaceBoolList;
            private set => interfaceBoolList = value;
        }

        public EvaluatorElement() : base()
        {
            InitInternal();
        }

        public EvaluatorElement(string label, bool b1, bool b2, bool b3, List<ParameterData> parameters)
        {
            InitInternal();
            SetEvaluatorElement(label, b1, b2, b3);
            this.parameters = parameters;
        }

        private void InitInternal()
        {
            GetVisualTreeForThis();

            evLabel = this.Q<LBSCustomLabel>("evName");

            evDeleteButton = this.Q<LBSCustomButton>("evDelete");
            evDeleteButton.RegisterCallback<ClickEvent>(DeleteEvaluatorElement);
            evConfigButton = this.Q<LBSCustomButton>("evOptions");
            evConfigButton.RegisterCallback<ClickEvent>(OpenEvaluatorConfig);
            evOpenSOButton = this.Q<LBSCustomButton>("evOpenSO");
            evOpenSOButton.RegisterCallback<ClickEvent>(OpenSO);

            interfaceIcon1 = this.Q<VisualElement>("InterfaceIcon1");
            interfaceIcon2 = this.Q<VisualElement>("InterfaceIcon2");
            interfaceIcon3 = this.Q<VisualElement>("InterfaceIcon3");

            interfaceBoolList = new List<bool>() { false, false, false };

            interfaceBoolListVisualElements = new List<VisualElement>
            {
                interfaceIcon1,
                interfaceIcon2,
                interfaceIcon3
            };
        }

        public void SetEvaluatorElement(string label, bool b1, bool b2, bool b3)
        {
            EvLabelString = label;
            SetInterfaceBooleanList(b1, b2, b3);
            SetSOButtonVisibility();
        }
        public void SetInterfaceBooleanList(bool b1, bool b2, bool b3)
        {
            interfaceBoolList[0] = b1;
            interfaceBoolList[1] = b2;
            interfaceBoolList[2] = b3;
            SetInterfaceIconVisibility(b1, b2, b3);
        }
        public void SetInterfaceBooleanByIndex(int i, bool b)
        {
            interfaceBoolList[i] = b;
            SetInterfaceIconVisibilitybyIndex(i, b);
        }
        public void SetInterfaceIconVisibility(bool b1, bool b2, bool b3)
        {
            SetInterfaceIconVisibilitybyIndex(0,b1);
            SetInterfaceIconVisibilitybyIndex(1,b2);
            SetInterfaceIconVisibilitybyIndex(2,b3);
        }
        public void SetInterfaceIconVisibilitybyIndex(int i, bool b)
        {
            //interfaceBoolListVisualElements[i].visible = b;

            //gray icons instead of invisible icons
            if (!b)
            {
                //interfaceBoolListVisualElements[i].style.unityBackgroundImageTintColor = Color.gray;
                interfaceBoolListVisualElements[i].RemoveFromClassList("lbs-custom-colored-icon");
                interfaceBoolListVisualElements[i].AddToClassList("lbs-custom-colored-icon-disabled");
            }
            else 
            {
                interfaceBoolListVisualElements[i].RemoveFromClassList("lbs-custom-colored-icon-disabled");
                interfaceBoolListVisualElements[i].AddToClassList("lbs-custom-colored-icon");
            }
                
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private void DeleteEvaluatorElement(ClickEvent evt)
        {
            OnDelete?.Invoke(this);
        }

        private void OpenEvaluatorConfig(ClickEvent evt)
        {
            EvaluatorsParameterWindow existingWindow = Resources.FindObjectsOfTypeAll<EvaluatorsParameterWindow>().FirstOrDefault();
            
            // 2. Comparamos el título actual con el que queremos poner
            // Usamos la misma cadena exacta que definiste para el título
            string expectedTitle = $"{EvLabelString}";

            if (existingWindow != null)
            {
                if (existingWindow.titleContent.text != expectedTitle)
                {
                    // Si el título es diferente, cerramos la ventana vieja
                    existingWindow.Close();
                }
                else
                {
                    // Si es el mismo, solo le damos foco y no hacemos nada más
                    existingWindow.Focus();
                    return;
                }
            }

            EvaluatorsParameterWindow window = EditorWindow.GetWindow<EvaluatorsParameterWindow>(false, expectedTitle, true);
            window.ParameterList = parameters ;
            window.LoadParamVisualList();
            window.EvRef = EvLabelString;

            // 2. ASIGNAR EL ICONO
            // Opción A: Usar un icono interno de Unity (ej: un engranaje o una lista)
            // Algunos nombres útiles: "Settings", "d_Settings", "FilterByLabel", "CustomTool"
            Texture2D icon = EditorGUIUtility.IconContent("Settings").image as Texture2D;

            // Opción B: Usar tu propio icono desde una carpeta Resources o AssetDatabase
            // Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Path/To/YourIcon.png");

            window.titleContent = new GUIContent(expectedTitle, icon);

            window.minSize = new UnityEngine.Vector2(300, 84);
            window.Show();
        }
        public void OpenSO(ClickEvent evt)
        {
            string fullPath = LBSSettings.Instance.paths.assistantPresetFolderPath + "/Evaluators/" + evLabel.text + " configuration.asset";
            if (File.Exists(fullPath))
            {
            }
            else
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t = assembly.GetType("ISILab.AI.Categorization." + evLabel.text);
                    if (t != null)
                    {
                        object instance = Activator.CreateInstance(t);

                        if (instance is IConfigurableEvaluator cEvaluator)
                        {
                            cEvaluator.InitializeDefault();
                        }
                    }
                }
            }

            EditorApplication.delayCall += () =>
            {
                // Focus the Project window
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath);
                EditorUtility.FocusProjectWindow();
                // Select the object, which makes the project window jump to that folder
                Selection.activeObject = obj;
                // Optional: Ping the object to highlight it visually
                EditorGUIUtility.PingObject(obj);
            };
        }

        public void SetSOButtonVisibility()
        {
            if (!interfaceBoolList[0])
            {
                //evOpenSOButton.visible = false;

                //disabled button instead of invisible
                evOpenSOButton.SetEnabled(false);
                //evOpenSOButton.pickingMode = PickingMode.Ignore;
                //evOpenSOButton.focusable = false;
                //evOpenSOButton.style.unityBackgroundImageTintColor = Color.gray;

                evOpenSOButton.tooltip = "Open Scriptable Object.\nOnly available for configurable evaluators";

                evOpenSOButton.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    evt.StopImmediatePropagation();
                }, TrickleDown.TrickleDown);

                evOpenSOButton.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    evt.StopImmediatePropagation();
                }, TrickleDown.TrickleDown);

            }
        }
    }
}
