using System;

namespace ISILab.AI.Grammar
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GrammarFieldAttribute : Attribute
    {
        public string Id;          // "int". Identifier used in the xml file.
        public GrammarFieldAttribute(string id) => Id = id;
    }
}


