using System;
using ISILab.LBS.Modules;
using Newtonsoft.Json;
using UnityEditor.Graphs;
using UnityEngine;

[Serializable]
public class LBSNote : ICloneable
{
    #region FIELDS

    [SerializeField, JsonRequired]
    private string id = "";

    private static int noteCounter = 0;

    [SerializeField, JsonRequired]
    protected float x;

    [SerializeField, JsonRequired]
    protected float y;

    [SerializeField, JsonRequired]
    protected string message;

    #endregion

    #region PROPERTIES

    public string ID
    {
        get => id;
        set => id = value;
    }

    public Vector2 Position
    {
        get => new(x, y);
        set
        {
            x = value.x;
            y = value.y;
        }
    }

    public string Message
    {
        get => message;
        set => message = value;
    }

    #endregion

    #region CONSTRUCTORS

    protected LBSNote() { }

    public LBSNote(Vector2 position, string message)
    {
        id = $"Note {++noteCounter}";
        x = position.x;
        y = position.y;
        this.message = message;
    }

    #endregion

    public object Clone()
    {
        var clone = new LBSNote();

        clone.ID = ID;
        clone.x = x;
        clone.y = y;
        clone.message = message;

        return clone;
    }
}
