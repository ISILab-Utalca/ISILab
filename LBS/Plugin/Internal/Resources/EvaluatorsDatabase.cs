using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using System;
using System.Collections.Generic;
using UnityEngine;

#region STRUCTURES

[System.Serializable]
public struct EvaluatorData
{
    public string name;
    public bool interface1;
    public bool interface2;
    public bool interface3;
    public List<ParameterData> paramList;

    public EvaluatorData(string name, bool i1, bool i2, bool i3)
    {
        this.name = name;
        interface1 = i1;
        interface2 = i2;
        interface3 = i3;
        paramList = new List<ParameterData>();
    }
}

[System.Serializable]
public struct ParameterData
{
    public string name;
    public Type varType;
    public string initialValue;
    public bool isDeletable;

    public ParameterData(string name, Type varType, string initialValue, bool isDeletable)
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

    /*
    public void CreateInstance()
    {
        evaluators = new List<EvaluatorData>();
    }
    */


    #region METHODS
    public EvaluatorData ReturnEvaluatorByName(string name)
    {
        if (evaluators.Exists(x => x.name == name))
        {
            return evaluators.Find(x => x.name == name);
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

    #endregion
}
