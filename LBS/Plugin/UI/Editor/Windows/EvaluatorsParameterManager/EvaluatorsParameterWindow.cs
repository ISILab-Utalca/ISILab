using ISILab.Commons.Utility.Editor;
using ISILab.LBS;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Core.Settings;
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
using UnityEngine.UIElements;
using static ISILab.LBS.Editor.PopulationAssistantTab;

public class EvaluatorsParameterWindow : ThemeableWindow
{
    #region VISUAL ELEMENTS
    //[SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private LBSCustomListView paramListView;
    //Parameter Generator
    private LBSCustomTextField paramGenName;
    private LBSCustomDropdown paramGenClassDropDown;
    private LBSCustomTextField paramGenInitialValue;
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
        ResetParamGenerator();
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
            "int",
            "float",
            "bool",
            "LBSCharacteristic",
            "List<LBSCharacteristic>"
        };

        paramGenInitialValue = rootVisualElement.Q<LBSCustomTextField>("ParamGenIValue");
        
        paramGenButton = rootVisualElement.Q<LBSCustomButton>("ParamGenButton");
        paramGenButton.RegisterCallback<ClickEvent>(GenerateNewParameter);

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
            paramGenInitialValue.value
            );

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
    //SEGUIR ACÁ
    public bool CheckIfParamInitialValueIsValid()
    {
        //si es característica o lista de característica, init value debería estar vacío
        if(paramGenClassDropDown.value == "int")
        {
            int i;
            return int.TryParse(paramGenInitialValue.value,out i);
        }
        else if (paramGenClassDropDown.value == "float")
        {
            float f;
            return float.TryParse(paramGenInitialValue.value, out f);
        }
        else if ((paramGenClassDropDown.value == "LBSCharacteristic" ||
           paramGenClassDropDown.value == "List<LBSCharacteristic>"))
        {
            return string.IsNullOrWhiteSpace(paramGenInitialValue.value);
        }
        else if (paramGenClassDropDown.value == "bool") //HACERLO NO CASE SENSITIVE
        {
            return (paramGenInitialValue.value.ToLower() == "true" || paramGenInitialValue.value.ToLower() == "false");
        }
        else return false;
     
            //"int",
            //"float",
    }
    private Type GetTypeFromString(string name)
    {
        switch (name)
        {
            case "int": return typeof(int);
            case "float": return typeof(float);
            case "bool": return typeof(bool);
            case "LBSCharacteristic": return typeof(LBSCharacteristic);
            case "List<LBSCharacteristic>": return typeof(List<LBSCharacteristic>);
            default: return typeof(int);
        }
    }
    public void ResetParamGenerator()
    {
        //paramGenName.value = "newParam";
        //paramGenClassDropDown.value = "int";
        //paramGenInitialValue.value = "";
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

    //SEGUIR ACÁ
    public void AddParamCode(ParameterData paramData)
    {
        UnityEngine.Object evToEdit = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(LBSSettings.Instance.paths.evaluatorsPath + EvRef);
        Type type = GetTypeFromString(paramData.varTypeAsString);
    }

    public void DeleteParamCode(ParameterData paramData)
    {
        UnityEngine.Object evToEdit = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(LBSSettings.Instance.paths.evaluatorsPath + EvRef);
    }
}
