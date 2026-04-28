using System;
using System.Collections;
using System.Collections.Generic;

namespace ISILab.AI.Grammar
{
    public interface GrammarListFieldMarker { }

    [Serializable]
    public abstract class GrammarField : ICloneable
    {
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
                "referencetype" => new GrammarType { name = name },
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
                value = typedValue;
            }
            else
            {
                try
                {
                    value = (T)Convert.ChangeType(newValue, typeof(T));
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

    [Serializable] public class GrammarInt : GrammarField<int> { public override Type PrimitiveType => typeof(GrammarInt); }
    [Serializable] public class GrammarFloat : GrammarField<float> { public override Type PrimitiveType => typeof(GrammarFloat); }
    [Serializable] public class GrammarString : GrammarField<string> { public override Type PrimitiveType => typeof(GrammarString); }
    [Serializable] public class GrammarObject : GrammarField<UnityEngine.Object> { public override Type PrimitiveType => typeof(GrammarObject); }
    [Serializable] public class GrammarType : GrammarField<string> { public override Type PrimitiveType => typeof(GrammarType); }

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
    public class GrammarTypeList : GrammarListField<GrammarType>
    {
        public override Type PrimitiveType => typeof(GrammarType);
    }
    #endregion
}