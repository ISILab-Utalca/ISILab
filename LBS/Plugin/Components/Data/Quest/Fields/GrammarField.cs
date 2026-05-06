using ISILab.LBS.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ISILab.AI.Grammar
{
    public interface GrammarListFieldMarker { }

    [Serializable]
    public abstract class GrammarField : ICloneable
    {
        #region FIELDS
        private static readonly Dictionary<string, Type> fieldMap = new();
        public QuestNodeData data;
        public string name;
        #endregion

        #region ACTIONS
        // used main to broadcast unto visual elements whenever the value changes.
        public Action Refresh;
        #endregion

        #region PROPERTIES
        /// Primitive type used by this field
        /// GrammarIntList -> GrammarInt
        /// GrammarInt     -> GrammarInt
        public abstract Type PrimitiveType { get; }

        /// True if this is a list field
        public bool IsList => this is GrammarListFieldMarker;

        /// Generic access for ListView.itemsSource
        public virtual IList ItemsSource => null;
        #endregion

        #region METHODS

        // on unity engine load
        static GrammarField()
        {
            fieldMap.Clear();
            var grammarFieldTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes());

            foreach (var gft in grammarFieldTypes)
            {
                if (gft.IsAbstract) continue;

                var attr = gft.GetCustomAttributes(typeof(GrammarFieldAttribute), false)
                            .FirstOrDefault() as GrammarFieldAttribute;

                if (attr == null) continue;

                if (!fieldMap.ContainsKey(attr.Id))
                {
                    fieldMap[attr.Id] = gft;
                }
            }
        }
        public static GrammarField CreateField(string type, string name)
        {
            if (!fieldMap.TryGetValue(type, out var fieldType))
                throw new Exception($"Unknown field type: {type}. No grammar field class found." +
                    $"\nMake sure to add a GrammarAttribute to the class.");

            var instance = (GrammarField)Activator.CreateInstance(fieldType);
            instance.name = name;
            return instance;
        }

        public abstract object Clone();
        public virtual void SetValue(object newValue) { }
        public virtual object GetValue() => null;
        public override int GetHashCode() => base.GetHashCode() + name.GetHashCode() + data.GetHashCode();
        public override string ToString() => data.Node.ID + ": " + name;
        #endregion
    }
}