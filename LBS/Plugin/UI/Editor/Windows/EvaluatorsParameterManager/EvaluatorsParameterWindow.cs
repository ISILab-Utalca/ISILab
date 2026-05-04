using ISILab.Commons.Utility.Editor;
using ISILab.LBS;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Internal;
using ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EvaluatorElement;
using ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EVParameterElement;
using ISILab.LBS.Plugin.UI.Editor.Windows;
using ISILab.LBS.VisualElements;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Configuration;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class EvaluatorsParameterWindow : ThemeableWindow
{
    #region STRUCTURES
    public enum IValueType
    {
        Int,
        Float,
        Bool,
        LBSTag,
        LBSTagList // Representa List<LBSTag>
    }
    #endregion

    #region VISUAL ELEMENTS
    //[SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private LBSCustomListView paramListView;
    //Parameter Generator
    private LBSCustomTextField paramGenName;
    private LBSCustomDropdown paramGenClassDropDown;
    private LBSCustomDropdown paramGenIValueDropDown;
    private LBSCustomTextField paramGenInitialValueText;
    private LBSCustomButton paramGenButton;
    #endregion

    #region FIELDS
    private EvaluatorsDatabase evDatabase;
    private List<ParameterData> parameterList;
    private string evRef;
    #endregion

    #region PROPERTIES
    public List<ParameterData> ParameterList
    {
        get { return parameterList; }
        set { 
            parameterList = value;
            RefreshData();
        }
    }
    public string EvRef
    {
        get { return evRef; }
        set { evRef = value; }
    }
    #endregion

    #region METHODS
    public new void CreateGUI()
    {
        FindDatabase();

        m_VisualTreeAsset = DirectoryTools.GetAssetByName<VisualTreeAsset>("EvaluatorsParameterWindow");
        if (m_VisualTreeAsset != null)
            m_VisualTreeAsset.CloneTree(rootVisualElement);

        var styleSheet = DirectoryTools.GetAssetByName<StyleSheet>("LBSMainTheme");

        if (styleSheet != null)
        {
            rootVisualElement.styleSheets.Add(styleSheet);
        }
       
        InitUI();
        InitParamGenerator();
        RefreshData();
        ChangeTheme(LBSSettings.Instance.view.LBSTheme);
    }

    public void InitUI()
    {
        paramListView = rootVisualElement.Q<LBSCustomListView>("ParamList");
        paramGenName = rootVisualElement.Q<LBSCustomTextField>("ParamGenName");
        paramGenClassDropDown = rootVisualElement.Q<LBSCustomDropdown>("ParamGenDD");
        paramGenClassDropDown.choices = new List<string>
        {
            IValueType.Int.AsString(),
            IValueType.Float.AsString(),
            IValueType.Bool.AsString(),
            IValueType.LBSTag.AsString(),
            IValueType.LBSTagList.AsString()
        };
        paramGenClassDropDown.RegisterValueChangedCallback(evt =>
        {
            ManageIValueUI(evt.newValue);
        });

        paramGenInitialValueText = rootVisualElement.Q<LBSCustomTextField>("ParamGenIValue");
        paramGenIValueDropDown = rootVisualElement.Q<LBSCustomDropdown>("paramGenLBSCharDropDown");

        paramGenButton = rootVisualElement.Q<LBSCustomButton>("ParamGenButton");
        paramGenButton.RegisterCallback<ClickEvent>(GenerateNewParameter);

        //"TÍTULO PARA LA LISTA"
        EVParameterElement paramVE = new EVParameterElement("Name", false, "Type", "Initial Value");
        paramVE.style.unityFontStyleAndWeight = FontStyle.Bold;
        paramListView.hierarchy.ElementAt(0).Add(paramVE);


    }
    public void FindDatabase()
    {
        if (evDatabase == null)
        {
            // Busca cualquier asset que sea del tipo EvaluatorsDatabase
            string[] guids = AssetDatabase.FindAssets("t:EvaluatorsDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                evDatabase = AssetDatabase.LoadAssetAtPath<EvaluatorsDatabase>(path);
            }
        }
    }
    public void GenerateNewParameter(ClickEvent evt)
    {
        paramGenName.value = LBSTextUtilities.ReturnValidName(paramGenName.value);
        //if (!string.IsNullOrWhiteSpace(paramGenName.value) && !CheckIfParamInitialValueIsValid())
        if (!string.IsNullOrWhiteSpace(paramGenName.value))
        {
            if (CheckIfParamInitialValueIsValid())
            {
                ParameterData paramToCreate = ReturnNewParameter();
                // generate param code
                AddParamCode(paramToCreate);
                // add new param to paramlist
                ParameterList.Add(paramToCreate);
                AddParamToVisualList(paramToCreate);
                // refresh
                RefreshData();
                ResetParamGenerator();
                // profit
                Debug.Log("Parámetro creado :-)");
                evDatabase.SaveDatabaseChanges();
            }
            else
            {
                bool confirm = EditorUtility.DisplayDialog(
                "Error",                                                    // Título
                "Invalid initial value",                                    // Mensaje
                "OK"                                                        // Botón de cancelar
                );
            }
        }
        else
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Error",                                                    // Título
                "Parameter's name cannot be empty or have special characters other than \"_\"",                         // Mensaje
                "OK"                                                        // Botón de cancelar
            );
        }
    }
    public ParameterData ReturnNewParameter()
    {
        ParameterData newParameter = new ParameterData(
                paramGenName.value,
                paramGenClassDropDown.value,
                ""
                );

        if (paramGenClassDropDown.value == IValueType.Int.AsString()|| paramGenClassDropDown.value == IValueType.Float.AsString())
        {
            newParameter.initialValue = paramGenInitialValueText.value;
        }
        else if (paramGenClassDropDown.value == IValueType.Bool.AsString() || paramGenClassDropDown.value == IValueType.LBSTag.AsString())
        {
            newParameter.initialValue = paramGenIValueDropDown.value;
        }
        else if (paramGenClassDropDown.value == IValueType.LBSTagList.AsString())
        {
            newParameter.initialValue = "";
        }

        return ReturnParamDataWUniqueName(newParameter);
    }
    public ParameterData ReturnParamDataWUniqueName(ParameterData paramData)
    {
        string newName = paramData.name;
        int counter = 0;
        while (!CheckUniqueEvName(newName))
        {
            counter++;
            newName = paramData.name + "_" + counter.ToString();
        }
        paramData.name = newName;
        return paramData;
    }
    public bool CheckUniqueEvName(string baseName)
    {
        bool isUniqueName = true;
        foreach (ParameterData evData in parameterList)
        {
            if (evData.name == baseName) isUniqueName = false;
        }

        return isUniqueName;
    }
    public bool CheckIfParamInitialValueIsValid()
    {
        //si es característica o lista de característica, init value debería estar vacío
        if(paramGenClassDropDown.value == IValueType.Int.AsString())
        {
            int i;
            return int.TryParse(paramGenInitialValueText.value,out i);
        }
        else if (paramGenClassDropDown.value == IValueType.Float.AsString())
        {
            float f;
            return float.TryParse(paramGenInitialValueText.value, out f);
        }
        else if ((paramGenClassDropDown.value == IValueType.Bool.AsString() ||
            paramGenClassDropDown.value == IValueType.LBSTag.AsString() ||
            paramGenClassDropDown.value == IValueType.LBSTagList.AsString()))
        {
            return !string.IsNullOrEmpty(paramGenIValueDropDown.value);
        }
        else return false;
    }
    private Type GetTypeFromString(string name)
    {
        switch (IValueTypeExtensions.asEnum(name))
        {
            case IValueType.Int: return typeof(int);
            case IValueType.Float: return typeof(float);
            case IValueType.Bool: return typeof(bool);
            case IValueType.LBSTag: return typeof(LBSTag);
            case IValueType.LBSTagList: return typeof(List<LBSTag>);
            default: return typeof(int);
        }
    }
    public void InitParamGenerator()
    {
        paramGenName.value = "newParam";
        paramGenClassDropDown.value = IValueType.Int.AsString();
        paramGenInitialValueText.value = "0";
        paramGenIValueDropDown.value = "";
    }
    public void ResetParamGenerator()
    {
    }
    public void RefreshData()
    {
        if (rootVisualElement == null) return;
        //UI refresh logic
    }
    public void LoadParamVisualList()
    {
        foreach (ParameterData param in parameterList)
        {
            AddParamToVisualList(param);
        }
    }
    public void AddParamToVisualList(ParameterData param)
    {
        //turn param into VisualElement
        EVParameterElement paramVE = new EVParameterElement(param.name, param.isDeletable, param.varTypeAsString, param.initialValue);

        paramVE.OnDelete += (elem) =>
            {
                // Mostramos el diálogo nativo de Unity
                bool confirm = EditorUtility.DisplayDialog(
                    "Eliminar Evaluador",               // Título
                    $"¿Estás seguro de que deseas eliminar el parámetro '{param.name}'?", // Mensaje
                    "Eliminar",                         // Botón de confirmar
                    "Cancelar"                          // Botón de cancelar
                );

                if (confirm)
                {
                    // Si el usuario acept?, lo borramos de la interfaz
                    // 'target' es el elemento que dispar? el evento
                    //elem.parent.hierarchy.Remove(elem); <- if i can do that why do all of this?
                    paramListView.hierarchy.ElementAt(0).Remove(elem);
                    parameterList.Remove(param);
                    DeleteParamCode(param);     
                    evDatabase.SaveDatabaseChanges();
                }
            };

        paramListView.hierarchy.ElementAt(0).Add(paramVE);
    }
    //estas son las funciones en las que el seba deberia añadir sus cosas,
    // ev ref es un string con el nombre del evaluador que se está editando
    // se puede llamar a GetTypeFromString(paramGenClassDropDown.value) para obtener el Type del parámetro
    public void AddParamCode(ParameterData paramData)
    {
        UnityEngine.Object evToEdit = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(LBSSettings.Instance.paths.evaluatorsPath + EvRef);
        Type type = GetTypeFromString(paramData.varTypeAsString);
    }
    public void DeleteParamCode(ParameterData paramData)
    {
        UnityEngine.Object evToEdit = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(LBSSettings.Instance.paths.evaluatorsPath + EvRef);
    }
    public void ManageIValueUI(string newValue)
    {
        switch (IValueTypeExtensions.asEnum(newValue))
        {
            case IValueType.Int:        ManageIValueUIOnOff(true, false);   break;
            case IValueType.Float:      ManageIValueUIOnOff(true, false);   break;
            case IValueType.Bool:       ManageIValueUIOnOff(false, true);   ManageIValueDropdrownValues(newValue); break;
            case IValueType.LBSTag:     ManageIValueUIOnOff(false, true);    ManageIValueDropdrownValues(newValue); break;
            case IValueType.LBSTagList: ManageIValueUIOnOff(false, false);  ManageIValueDropdrownValues(newValue); break;
        }
    }
    public void ManageIValueUIOnOff(bool textBool, bool dropDownValue)
    {
        if(textBool)
            paramGenInitialValueText.style.display = DisplayStyle.Flex;
        else paramGenInitialValueText.style.display = DisplayStyle.None;
        
        if(dropDownValue)
            paramGenIValueDropDown.style.display = DisplayStyle.Flex;
        else paramGenIValueDropDown.style.display = DisplayStyle.None;
    }
    public void ManageIValueDropdrownValues(string s)
    {
        switch (s)
        {
            case "bool": 
                paramGenIValueDropDown.choices = new List<string>{"true","false"};
                paramGenIValueDropDown.value = paramGenIValueDropDown.choices[0];
                break;
            
            case "LBSTag": 
                paramGenIValueDropDown.choices = LBSAssetsStorage.Instance.GetNames<LBSTag>();
                paramGenIValueDropDown.value = paramGenIValueDropDown.choices[0]; 
                break;

            case "List<LBSTag>": break;
        }
    }
    #endregion
}
public static class IValueTypeExtensions
{
    // Al usar 'this', la función aparece como una opción del enum
    public static string AsString(this EvaluatorsParameterWindow.IValueType type)
    {
        return type switch
        {
            EvaluatorsParameterWindow.IValueType.Int => "int",
            EvaluatorsParameterWindow.IValueType.Float => "float",
            EvaluatorsParameterWindow.IValueType.Bool => "bool",
            EvaluatorsParameterWindow.IValueType.LBSTag => "LBSTag",
            EvaluatorsParameterWindow.IValueType.LBSTagList => "List<LBSTag>",
            _ => "Unknown"
        };
    }

    public static EvaluatorsParameterWindow.IValueType asEnum(string value)
    {
        return value switch
        {
            "int" => EvaluatorsParameterWindow.IValueType.Int,
            "float" => EvaluatorsParameterWindow.IValueType.Float,
            "bool" => EvaluatorsParameterWindow.IValueType.Bool,
            "LBSTag" => EvaluatorsParameterWindow.IValueType.LBSTag,
            "List<LBSTag>" => EvaluatorsParameterWindow.IValueType.LBSTagList,

            _ => EvaluatorsParameterWindow.IValueType.Int // Valor por defecto
        };
    }
}
