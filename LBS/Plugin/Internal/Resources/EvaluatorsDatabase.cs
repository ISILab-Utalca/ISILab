using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#region STRUCTURES

[System.Serializable]
public class EvaluatorData
{
    [SerializeField] private string name;
    [SerializeField] private bool interface1;
    [SerializeField] private bool interface2;
    [SerializeField] private bool interface3;
    [SerializeField] private List<ParameterData> paramList;

    public EvaluatorData(string name, bool i1, bool i2, bool i3)
    {
        this.name = name;
        interface1 = i1;
        interface2 = i2;
        interface3 = i3;
        paramList = new List<ParameterData>();
    }

    public string Name
    {
        get { return name; }
        set { name = value; }
    }
    public List<ParameterData> ParamList
    {
        get { return paramList; }
        set { paramList = value; }
    }
    public void AddParam(ParameterData param)
    {
        paramList.Add(param);
    }
    public void RemoveParam(ParameterData param)
    {
        paramList.Remove(param);
    }
    public bool Interface1
    {
        get { return interface1; }
        set { interface1 = value; }
    }
    public bool Interface2
    {
        get { return interface2; }
        set { interface2 = value; }
    }
    public bool Interface3
    {
        get { return interface3; }
        set { interface3 = value; }
    }
}

[System.Serializable]
public struct ParameterData
{
    public string name;
    public Type varType;
    public string initialValue;
    public bool isDeletable;

    public ParameterData(string name, Type varType, string initialValue, bool isDeletable = true)
    {
        this.name = name;
        this.varType = varType;
        this.initialValue = initialValue;
        this.isDeletable = isDeletable;
    }
}

#endregion

public class EvaluatorsDatabase : ScriptableObject
{
    [SerializeField] private List<EvaluatorData> evaluators = new List<EvaluatorData>();

    public List<EvaluatorData> Evaluators
    {
        get => evaluators;
        private set => evaluators = value;
    }

    #region METHODS
    public EvaluatorData ReturnEvaluatorByName(string name)
    {
        if (evaluators.Exists(x => x.Name == name))
        {
            return evaluators.Find(x => x.Name == name);
        }
        else return default;
    }
    public void AddEvaluatorToDatabase(EvaluatorData evData)
    {
        evaluators.Add(evData);
    }
    public void RemoveEvaluatorFromDatabase(EvaluatorData item)
    {
        evaluators.Remove(item);
    }
    public void SaveDatabaseChanges()
    {
        // Marca el objeto como "sucio" para que Unity sepa que debe guardarlo
        EditorUtility.SetDirty(this);

        // Fuerza el guardado de los assets modificados en el disco
        AssetDatabase.SaveAssets();
    }
    #endregion
}
