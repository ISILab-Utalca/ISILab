using ISILab.LBS.Macros;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace ISILab.AI.Grammar
{


    [Serializable]
    public class GrammarData : ScriptableObject
    {
        [SerializeField]
        public List<GrammarRule> LBSRules = new();
        [SerializeField]
        public List<GrammarTerminal> LBSTerminals = new();
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
    public abstract class GrammarElement : ScriptableObject
    {
       
        [SerializeField]
        public string id;

        public abstract VectorImage GetIcon();
    }

    [Serializable]
    public class GrammarRule : GrammarElement
    {
        private const string ruleIconGuid = "d7b2c9af591bee4429f705ae7ae6abea";

        // a suequence of strings(id's of the terminal actions or rules)
        [SerializeField]
        public List<List<string>> Expansions = new();

        public override VectorImage GetIcon() 
            => LBSAssetMacro.LoadAssetByGuid<VectorImage>(ruleIconGuid);
    }

    /// <summary>
    /// The action that gest added into the graph for example:
    ///     go to
    ///     explore
    ///     kill
    /// </summary>
    [Serializable]
    public class GrammarTerminal : GrammarElement
    {
        private const string defaultTerminalIcon = "bb0770b945366c94c822cf3255eb885d";

        [SerializeField]
        private string iconGuid = defaultTerminalIcon;

        [SerializeField] 
        public List<GrammarField> fields = new();

        public override VectorImage GetIcon()
    => LBSAssetMacro.LoadAssetByGuid<VectorImage>(iconGuid);
    }

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