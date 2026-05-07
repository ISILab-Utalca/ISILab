using System;

namespace ISILab.AI.Grammar
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GrammarFieldEditorAttribute : Attribute
    {
        public Type EditorType { get; }
        public GrammarFieldEditorAttribute(Type editorType) => EditorType = editorType;
    }
}