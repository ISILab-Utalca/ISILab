using System;
using System.Collections;
using System.Collections.Generic;

namespace ISILab.AI.Grammar
{
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
}