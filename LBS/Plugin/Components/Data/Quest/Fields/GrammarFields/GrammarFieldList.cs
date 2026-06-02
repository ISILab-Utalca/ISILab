using ISILab.LBS.Plugin.Core.Settings;
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

        public override bool IsValid()
        {
            foreach (var item in ItemsSource)
            {
                if (item is GrammarField gf)
                    if (!gf.IsValid()) return false;
            }

            return true;
        }
        public override LBSLog GetValidStateLog()
        {
            if (IsValid())
                return base.GetValidStateLog();

            string invalidFields = string.Empty;
            foreach (var item in ItemsSource)
            {
                if (item is GrammarField gf)
                {
                    if (!gf.IsValid())
                    {
                        invalidFields += $"{gf.name}\n";
                    }
                }
            }

            return new LBSLog($"{name}: Invalid fields: {invalidFields}", UnityEngine.LogType.Error);
        }
    }
}