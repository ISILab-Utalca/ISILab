using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

namespace ISILab.AI.Grammar
{
    public interface GrammarListFieldMarker { }

    [Serializable]
    public abstract class GrammarField : ICloneable
    {
        public QuestNodeData data;
        public string name;

        /// Primitive type used by this field
        /// GrammarIntList -> GrammarInt
        /// GrammarInt     -> GrammarInt
        public abstract Type PrimitiveType { get; }

        /// True if this is a list field
        public bool IsList => this is GrammarListFieldMarker;

        /// Generic access for ListView.itemsSource
        public virtual IList ItemsSource => null;

        public static GrammarField CreateField(string type, string name)
        {
            if (type.StartsWith("List."))
            {
                string inner = type.Substring(5).ToLower();

                return inner switch
                {
                    "int" => new GrammarIntList { name = name },
                    "float" => new GrammarFloatList { name = name },
                    "string" => new GrammarStringList { name = name },
                    "referencetype" => new GrammarTypeList { name = name },
                    "referencegraph" => new GrammarObjectList { name = name },
                    _ => throw new Exception($"Unknown list type {inner}")
                };
            }

            return type.ToLower() switch
            {
                "int" => new GrammarInt { name = name },
                "float" => new GrammarFloat { name = name },
                "string" => new GrammarString { name = name },
                "referencetype" => new GrammarObjectType { name = name },
                "referencegraph" => new GrammarObject { name = name },
                _ => throw new Exception($"Unknown field type {type}")
            };
        }

        public abstract object Clone();
        public virtual void SetValue(object newValue) { }
        public virtual object GetValue() => null;
    }


    #region FIELDS
    [Serializable]
    public abstract class GrammarField<T> : GrammarField
    {
        public T value;

        public override object Clone()
        {
            var clone = (GrammarField<T>)Activator.CreateInstance(GetType());
            clone.name = name;
            clone.value = value;
            return clone;
        }
        public override void SetValue(object newValue)
        {
            if (newValue is T typedValue)
            {
                // call back to ctrz support. mark dirty in NodeDataBehaviorEditor
                data?.OnBeginChange?.Invoke();
                value = typedValue;
                data?.OnEndChange?.Invoke();


                data?.OnDataChanged?.Invoke(data);
            }
            else
            {
                try
                {
                    data?.OnBeginChange?.Invoke();
                    value = (T)Convert.ChangeType(newValue, typeof(T));
                    data?.OnEndChange?.Invoke();


                    data?.OnDataChanged?.Invoke(data);
                }
                catch
                {
                    UnityEngine.Debug.LogError($"[Grammar] Cannot assign {newValue?.GetType()} to {typeof(T)}");
                }
            }
        }

        public override object GetValue() => value;
    }

    [Serializable]
    public abstract class GrammarListField<TField> : GrammarField, GrammarListFieldMarker
    where TField : GrammarField, new()
    {
        public List<TField> value = new();

        public override IList ItemsSource => value;

        public override object Clone()
        {
            var clone = (GrammarListField<TField>)Activator.CreateInstance(GetType());
            clone.name = name;

            foreach (var item in value)
                clone.value.Add((TField)item.Clone());

            return clone;
        }

        public override void SetValue(object newValue)
        {
            if (newValue is List<TField> list)
                value = list;
        }
    }

    #endregion

    #region PRIMITIVES
    [Serializable] public class GrammarEventHook : GrammarField<LBSEventHooker> 
    {
        public override Type PrimitiveType => typeof(LBSEventHooker);
    }
    [Serializable] public class GrammarArea : GrammarField<Rect>
    {
        public override Type PrimitiveType => typeof(Rect);
    }
    [Serializable] public class GrammarInt : GrammarField<int> 
    { 
        public override Type PrimitiveType => typeof(GrammarInt); 
    }
    [Serializable] public class GrammarFloat : GrammarField<float> 
    { 
        public override Type PrimitiveType => typeof(GrammarFloat); 
    }
    [Serializable] public class GrammarString : GrammarField<string> 
    { 
        public override Type PrimitiveType => typeof(GrammarString); 
    }
    [Serializable]
    public class GrammarObject : GrammarField<BundleTargetGraph>
    {
        public override Type PrimitiveType => typeof(GrammarObject);

        public override void SetValue(object newValue)
        {
            if (newValue is BundleTargetGraph target)
            {
                value = target;
            }
        }

        public override object GetValue() => value;
    }
    [Serializable] public class GrammarObjectType : GrammarField<string> { public override Type PrimitiveType => typeof(GrammarObjectType); }

    #endregion

    #region LISTS

    [Serializable]
    public class GrammarIntList : GrammarListField<GrammarInt>
    {
        public override Type PrimitiveType => typeof(GrammarInt);
    }

    [Serializable]
    public class GrammarFloatList : GrammarListField<GrammarFloat>
    {
        public override Type PrimitiveType => typeof(GrammarFloat);
    }

    [Serializable]
    public class GrammarStringList : GrammarListField<GrammarString>
    {
        public override Type PrimitiveType => typeof(GrammarString);
    }

    [Serializable]
    public class GrammarObjectList : GrammarListField<GrammarObject>
    {
        public override Type PrimitiveType => typeof(GrammarObject);
    }

    [Serializable]
    public class GrammarTypeList : GrammarListField<GrammarObjectType>
    {
        public override Type PrimitiveType => typeof(GrammarObjectType);
    }
    #endregion
}