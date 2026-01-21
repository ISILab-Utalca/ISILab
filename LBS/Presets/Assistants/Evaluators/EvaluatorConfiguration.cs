using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using ISILab.LBS.Plugin.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.AI.Categorization
{
    [Serializable]
    [CreateAssetMenu(menuName = "ISILab/LBS/Evaluator Configuration")]
    public class EvaluatorConfiguration : ScriptableObject
    {
        [Serializable]
        public abstract class EvaluatorConfigurationField
        {
            public string name;

            public EvaluatorConfigurationField(string fieldName)
            {
                name = fieldName;
            }

            public abstract VisualElement GetField();
            protected abstract void SetField();
            public abstract object GetValue();

            public override bool Equals(object obj)
            {
                return Equals(name, (obj as EvaluatorConfigurationField)?.name);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(name);
            }
        }

        [Serializable]
        public class MainTagField : EvaluatorConfigurationField
        {
            [SerializeField]
            string tagName;
            [SerializeField]
            LBSCharacteristic tagChar;

            ObjectField field;

            public LBSCharacteristic TagChar
            {
                get
                {
                    tagChar ??= new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag(tagName));
                    return tagChar;
                }
            }

            public ObjectField Field
            {
                get
                {
                    if (field is null)
                        SetField();
                    return field;
                }
            }

            public MainTagField(string fieldName, LBSCharacteristic tagChar) : this(fieldName, fieldName, tagChar) { }

            public MainTagField(Tuple<string, LBSCharacteristic> nameAndChar) : this("", nameAndChar.Item1, nameAndChar.Item2) { }

            public MainTagField(string fieldName, string tagName, LBSCharacteristic tagChar) : base(fieldName)
            {
                var t = tagChar.GetType();
                UnityEngine.Assertions.Assert.IsTrue(t.Equals(typeof(LBSTagsCharacteristic)));

                this.tagName = tagName;
                this.tagChar = tagChar;

                var t2 = this.tagChar.GetType();
                UnityEngine.Assertions.Assert.IsTrue(t2.Equals(typeof(LBSTagsCharacteristic)));

                SetField();
            }

            public override VisualElement GetField() => Field;
            protected override void SetField()
            {
                field = new ObjectField(name);
                field.objectType = typeof(LBSTag);
                if(field.value == null)
                    field.SetValueWithoutNotify(LBSAssetMacro.GetLBSTag(tagName));
                field.RegisterValueChangedCallback(evt =>
                {
                    LBSTag tag = evt.newValue as LBSTag;
                    tagChar = new LBSTagsCharacteristic(tag);
                    tagName = tag.Label;
                });
            }

            public override object GetValue() => TagChar;//Field.value;
        }

        [Serializable]
        public class GroupedTagsField : EvaluatorConfigurationField
        {
            [SerializeField]
            List<MainTagField> tagsFields = new();
            
            ListView list;

            public ListView List
            {
                get
                {
                    if (list == null)
                        SetField();
                    return list;
                }
            }

            public GroupedTagsField(string groupName, List<Tuple<string, LBSCharacteristic>> tagsNamesAndChars) : base(groupName)
            {
                foreach(Tuple<string, LBSCharacteristic> nameAndChar in tagsNamesAndChars)
                    tagsFields.Add(new MainTagField(nameAndChar));

                SetField();
            }

            public override VisualElement GetField() => List;
            protected override void SetField()
            {
                list = new ListView();
                list.itemsSource = tagsFields;
                list.makeItem = () => new VisualElement();
                list.bindItem = (item, i) =>
                {
                    item.Clear();
                    item.Add((list.itemsSource[i] as MainTagField).GetField());
                };
            }

            public override object GetValue() => tagsFields.Select(tag => tag.GetValue());
        }

        [SerializeReference]
        public object target;
        [SerializeReference]
        public List<EvaluatorConfigurationField> fields = new();

        public static void CreateOrUpdateConfiguration(ref EvaluatorConfiguration config, Type type, Func<List<EvaluatorConfigurationField>> getFields = null)
        {
            string path = LBSSettings.Instance.paths.assistantPresetFolderPath + "/Evaluators";
            string assetName = type.Name;
            string fullPath = path + "/" + assetName + " configuration.asset";

            if (AssetDatabase.AssetPathExists(fullPath))
            {
                config = AssetDatabase.LoadAssetAtPath<EvaluatorConfiguration>(fullPath);
            }
            else
            {
                config = ScriptableObject.CreateInstance<EvaluatorConfiguration>();
                AssetDatabase.CreateAsset(config, fullPath);
                AssetDatabase.SaveAssets();
            }

            config.target ??= Activator.CreateInstance(type);

            if(getFields is not null)
            {
                config.fields.Clear();
                config.fields.AddRange(getFields?.Invoke());

                var conf = config;
                Selection.activeObject = null;
                EditorApplication.delayCall += () => Selection.activeObject = conf;
            }
        }

        public T GetValue<T>(string name)
        {
            return (T)fields.Find(f => f.name.Equals(name)).GetValue();
        }

        public IEnumerable<T> GetValues<T>(string name)
        {
            object val = fields.Find(f => f.name.Equals(name)).GetValue();
            var t = val.GetType();
            //UnityEngine.Assertions.Assert.IsTrue(t.Equals(typeof(LBSTagsCharacteristic)));

            var ret = (val as IEnumerable<object>).Select(v => (T)v);
            return ret;
        }
    }
}
