using System;
using System.Collections.Generic;

namespace ISILab.AI.Grammar
{

    [Serializable]
    public class GrammarField
    {
        public string name;

        public static GrammarField CreateField(string type, string name)
        {
            // lists
            if (type.StartsWith("List."))
            {
                string inner = type.Substring(5);
                return inner switch
                {
                    "int" => new GrammarIntList { name = name },
                    "float" => new GrammarFloatList { name = name },
                    "string" => new GrammarStringList { name = name },
                    "referenceType" => new GrammarTypeList { name = name },
                    "referenceGraph" => new GrammarObjectList { name = name },
                    _ => throw new Exception($"Unknown list type: {inner}")
                };
            }

            // primitives
            return type switch
            {
                "int" => new GrammarInt { name = name },
                "float" => new GrammarFloat { name = name },
                "string" => new GrammarString { name = name },
                "referenceType" => new GrammarType { name = name },
                "referenceGraph" => new GrammarObject { name = name },
                _ => throw new Exception($"Unknown field type: {type}")
            };
        }

        public static List<GrammarField> Copy(IEnumerable<GrammarField> original)
        {
            if (original == null) return new List<GrammarField>();

            var result = new List<GrammarField>();

            foreach (var field in original)
            {
                var type = GetFieldType(field);
                var copy = CreateField(type, field.name);
                result.Add(copy);
            }

            return result;
        }

        private static string GetFieldType(GrammarField field)
        {
            return field switch
            {
                GrammarInt => "int",
                GrammarFloat => "float",
                GrammarString => "string",
                GrammarObject => "referenceGraph",
                GrammarType => "referenceType",

                GrammarIntList => "List.int",
                GrammarFloatList => "List.float",
                GrammarStringList => "List.string",
                GrammarObjectList => "List.referenceGraph",
                GrammarTypeList => "List.referenceType",

                _ => throw new Exception($"Unknown field type: {field.GetType()}")
            };
        }
    }




    #region Fields

    [Serializable]
    public abstract class GrammarField<T> : GrammarField
    {
        public T value;
    }

    [Serializable]
    public class GrammarInt : GrammarField<int> { }

    [Serializable]
    public class GrammarFloat : GrammarField<float> { }

    [Serializable]
    public class GrammarString : GrammarField<string> { }

    [Serializable]
    public class GrammarObject : GrammarField<UnityEngine.Object> { }

    [Serializable]
    public class GrammarType : GrammarField<Type> { }

    #endregion

    #region List fields

    [Serializable]
    public abstract class GrammarListField<T> : GrammarField
    {
        public List<T> value;
    }

    [Serializable]
    public class GrammarIntList : GrammarListField<int> { }

    [Serializable]
    public class GrammarFloatList : GrammarListField<float> { }

    [Serializable]
    public class GrammarStringList : GrammarListField<string> { }

    [Serializable]
    public class GrammarObjectList : GrammarListField<UnityEngine.Object> { }

    [Serializable]
    public class GrammarTypeList : GrammarListField<Type> { }

    #endregion
}