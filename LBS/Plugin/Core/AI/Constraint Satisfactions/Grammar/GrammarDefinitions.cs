using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ISILab.AI.Grammar
{
   
    [Serializable]
    public class GrammarRule
    {
        [SerializeField]
        public string ruleID;

        [SerializeField]
        public List<GrammarRule> definitions = new();
    }

    [Serializable]
    public class GrammarStructure : ScriptableObject
    {
        [SerializeField]
        public List<GrammarRule> Rules = new();

        [SerializeField]
        public List<GrammarTerminal> terminals = new();
    }

    /// <summary>
    /// A rule contains definitions, which would be used to expand the rule into terminal actions
    /// or more rules within. For example:
    /// GoTo:
    ///     go to
    ///     go to -> explore
    ///     Get -> go to
    ///     
    /// Get:
    ///     go to -> steal
    ///     go to -> take
    /// </summary>

    [Serializable]
    public class GrammarRule
    {
        [SerializeField]
        public string id;

        [SerializeField]
        public List<List<string>> Expansions = new();
    }


    /// <summary>
    /// The action that gest added into the graph for example:
    ///     go to
    ///     explore
    ///     kill
    /// </summary>
    [Serializable]
    public class GrammarTerminal : ScriptableObject
    {
        public string id;
        public List<GrammarField> fields = new();
    }

    [Serializable]
    public class GrammarField
    {
        public string name;

        public static GrammarField CreateField(string type, string name)
        {
            return type switch
            {
                "int" => new GrammarInt { name = name },
                "float" => new GrammarFloat { name = name },
                "string" => new GrammarString { name = name },
                "referenceType" => new GrammarType { name = name },
                "referenceGraph" => new GrammarObject { name = name },

                "intList" => new GrammarIntList { name = name },
                "floatList" => new GrammarFloatList { name = name },
                "stringList" => new GrammarStringList { name = name },

                _ => throw new Exception($"Unknown field type: {type}")
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