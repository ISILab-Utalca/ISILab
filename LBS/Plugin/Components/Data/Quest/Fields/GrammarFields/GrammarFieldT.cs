using ISILab.LBS.Plugin.Components.Bundles;
using System;
using UnityEngine;

namespace ISILab.AI.Grammar
{
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

                // to update behavior editors
                data?.OnDataChanged?.Invoke(data);

                // to update field editor
                Refresh?.Invoke();
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
}
