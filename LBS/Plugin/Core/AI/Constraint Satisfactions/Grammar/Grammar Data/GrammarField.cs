using System;
using System.Collections.Generic;

namespace ISILab.AI.Grammar
{
    [Serializable]
    public abstract class GrammarField
    {
        public string name;
        public abstract GrammarField DeepCopy();

        public static GrammarField CreateField(string type, string name)
        {
            // lists of primitives
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
                    _ => throw new Exception($"Unknown list type: {inner}")
                };
            }
            // primitives
            return type.ToLower() switch
            {
                "int" => new GrammarInt { name = name },
                "float" => new GrammarFloat { name = name },
                "string" => new GrammarString { name = name },
                "referencetype" => new GrammarType { name = name },
                "referencegraph" => new GrammarObject { name = name },
                _ => throw new Exception($"Unknown field type: {type}")
            };
        }
    }

    #region Types of fields

    [Serializable]
    public abstract class GrammarField<T> : GrammarField
    {
        public T value;

        // for primitives/structs. 
        public override GrammarField DeepCopy()
        {
            var instance = (GrammarField<T>)Activator.CreateInstance(this.GetType());
            instance.name = this.name;
            instance.value = this.value;
            return instance;
        }
    }

    [Serializable]
    public abstract class GrammarListField<T> : GrammarField
    {
        public List<T> value = new List<T>();

        public override GrammarField DeepCopy()
        {
            var instance = (GrammarListField<T>)Activator.CreateInstance(this.GetType());
            instance.name = this.name;
            // Create a new list and copy elements == deep list copy
            instance.value = new List<T>(this.value);
            return instance;
        }
    }

    #endregion

    #region Primitive Fields

    [Serializable] public class GrammarInt : GrammarField<int> { }

    [Serializable] public class GrammarFloat : GrammarField<float> { }

    [Serializable] public class GrammarString : GrammarField<string> { }

    [Serializable] public class GrammarObject : GrammarField<UnityEngine.Object> { }

    [Serializable] public class GrammarType : GrammarField<string> { } 

    #endregion

    #region List Fields

    [Serializable] public class GrammarIntList : GrammarListField<int> { }

    [Serializable] public class GrammarFloatList : GrammarListField<float> { }

    [Serializable] public class GrammarStringList : GrammarListField<string> { }

    [Serializable] public class GrammarObjectList : GrammarListField<UnityEngine.Object> { }

    [Serializable] public class GrammarTypeList : GrammarListField<string> { }

    #endregion
}