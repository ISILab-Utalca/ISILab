using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// A list container that can be serialized by Unity 
/// </summary>
/// <typeparam name="T"></typeparam>
[System.Serializable]
public class NestedList<T>
{
    [SerializeField]
    public List<T> list = new();

    public T this[int index]
    {
        get { return list[index]; }
        set { list[index] = value; }
    }

    public void Add(T item) => list.Add(item);
}
