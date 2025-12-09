using ISILab.Commons.Utility;
using ISILab.LBS.Editor;
using LBS.Components.TileMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("BundleData", typeof(BundleData))]
    public class BundleDataEditor : LBSCustomEditor
    {
        Dictionary<Type, Type> TypeToEditorMap = new();

        public override void SetInfo(object paramTarget)
        {
            this.target = paramTarget;
            CreateVisualElement();
        }

        protected override VisualElement CreateVisualElement()
        {
            var data = target as BundleData;

            Add(new Label(data.BundleName));

            Add(new Label("Characteristics"));

            foreach (var c in data.Characteristics)
            {
                if (c == null)
                {
                    Add(new Label("[NULL]"));
                    continue;
                }

                // Get type of element
                Type type = c.GetType();
                Type edtr = null;

                if (TypeToEditorMap.TryGetValue(type, out var editor))
                {
                    edtr = editor;
                }
                else
                {
                    // Get the editors of the selectable elements
                    var ves = Reflection.GetClassesWith<LBSCustomEditorAttribute>()
                            .Where(t => t.Item2.Any(v => v.type == type)).ToList();

                    if (ves.Count <= 0)
                    {
                        // Add basic label if no have specific editor
                        Add(new Label("'" + type + "' does not contain a visualization."));
                        continue;
                    }

                    // Get editor class
                    edtr = ves.First().Item1;
                    TypeToEditorMap.Add(type, edtr);
                }

                // Instantiate editor class
                LBSCustomEditor ve = Activator.CreateInstance(edtr, new object[] { c }) as LBSCustomEditor;
               // Debug.Log(ve.GetType());
                Add(ve);
            }

            return this;
        }
    }
}