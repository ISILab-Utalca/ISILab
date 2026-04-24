using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using System;
using System.Collections.Generic;
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

        private string evlabelString;
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

            //Here i manually set the "Scriptable Object" Icon while we dont have a vector option
            Texture2D soIcon = EditorGUIUtility.IconContent("ScriptableObject On Icon").image as Texture2D;
            evOpenSOButton.style.backgroundImage = new StyleBackground(soIcon);
            evOpenSOButton.style.backgroundSize = new StyleBackgroundSize(
                new BackgroundSize(new Length(80, LengthUnit.Percent), new Length(80, LengthUnit.Percent)));


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
            /*
            interfaceBoolListVisualElements[0].visible = b1;
            interfaceBoolListVisualElements[1].visible = b2;
            interfaceBoolListVisualElements[2].visible = b3;
            */
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
                interfaceBoolListVisualElements[i].style.unityBackgroundImageTintColor = Color.gray;
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
            string expectedTitle = $"Param Config: {EvLabelString}";

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

            // 2. ASIGNAR EL ICONO
            // Opción A: Usar un icono interno de Unity (ej: un engranaje o una lista)
            // Algunos nombres útiles: "Settings", "d_Settings", "FilterByLabel", "CustomTool"
            Texture2D icon = EditorGUIUtility.IconContent("Settings").image as Texture2D;

            // Opción B: Usar tu propio icono desde una carpeta Resources o AssetDatabase
            // Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Path/To/YourIcon.png");

            window.titleContent = new GUIContent(expectedTitle, icon);

            window.minSize = new UnityEngine.Vector2(300, 300);
            window.Show();
        }
        public void OpenSO(ClickEvent evt)
        {

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
