using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Macros;
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
    /// <summary>
    /// Scriptable Object that represents the current configuration data of a MAP Elites evaluator. Every instance of an evaluator uses the same configuration.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(menuName = "ISILab/LBS/Evaluator Configuration")]
    public class EvaluatorConfiguration : ScriptableObject
    {
        /// <summary>
        /// Abstract base class for a field of a configurable evaluator.
        /// </summary>
        [Serializable]
        public abstract class EvaluatorConfigurationField
        {
            /// <summary>
            /// Identifier of the field.
            /// </summary>
            public string name;

            /// <summary>
            /// Short description of the field's purpose.
            /// </summary>
            public string tooltip;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="fieldName">Identifier of the field.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public EvaluatorConfigurationField(string fieldName, string tooltip = "")
            {
                name = fieldName;
                this.tooltip = tooltip;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns>The Visual Element representing the configurable field.</returns>
            public abstract VisualElement GetField();
            /// <summary>
            /// Initializes the Visual Element that represents the configurable field and sets its value.
            /// </summary>
            protected abstract void SetField();
            /// <summary>
            /// 
            /// </summary>
            /// <returns>The value contained in the field as an object.</returns>
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

        /// <summary>
        /// Evaluator Configuration Field that represents a single tag.
        /// </summary>
        [Serializable]
        public class MainTagField : EvaluatorConfigurationField
        {
            /// <summary>
            /// Name of the LBSTag stored in the field.
            /// </summary>
            [SerializeField]
            string tagName;
            /// <summary>
            /// Characteristic containing the LBSTag.
            /// </summary>
            [SerializeField]
            LBSCharacteristic tagChar;

            /// <summary>
            /// Object Field containing a single LBSTag. <br />
            /// It mantains this class' other fields updated.
            /// </summary>
            ObjectField field;

            /// <summary>
            /// Property for <see cref="tagChar"/>. <br />
            /// If null, searches for the tag using <see cref="tagName"/>.
            /// </summary>
            public LBSCharacteristic TagChar
            {
                get
                {
                    tagChar ??= new LBSTagsCharacteristic(LBSAssetMacro.GetLBSTag(tagName));
                    return tagChar;
                }
            }

            /// <summary>
            /// Property for <see cref="field"/>. <br />
            /// If null, it initializes a new Object Field.
            /// </summary>
            public ObjectField Field
            {
                get
                {
                    if (field is null)
                        SetField();
                    return field;
                }
            }

            /// <summary>
            /// <b>The standard constructor.</b>
            /// </summary>
            /// <param name="fieldName">Identifier of the field.</param>
            /// <param name="tagName">Name of the LBSTag.</param>
            /// <param name="tagChar">Characteristic containing the LBSTag.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public MainTagField(string fieldName, string tagName, LBSCharacteristic tagChar, string tooltip = "") : base(fieldName, tooltip)
            {
                this.tagName = tagName;
                this.tagChar = tagChar;
                SetField();
            }

            /// <summary>
            /// Simplified constructor. Uses the field name as both the field and tag names.
            /// </summary>
            /// <param name="fieldName">Identifier of the field <b>and</b> tag.</param>
            /// <param name="tagChar">Characteristic containing the LBSTag.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public MainTagField(string fieldName, LBSCharacteristic tagChar, string tooltip = "") : this(fieldName, fieldName, tagChar, tooltip) { }

            /// <summary>
            /// Constructor for a special case of Tag Field that does not uses a name.
            /// </summary>
            /// <param name="tagNameAndChar">A tuple containing the name of the LBSTag and its characteristic.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public MainTagField(Tuple<string, LBSCharacteristic> tagNameAndChar, string tooltip = "") : this("", tagNameAndChar.Item1, tagNameAndChar.Item2, tooltip) { }

            public override VisualElement GetField() => Field;

            protected override void SetField()
            {
                field = new ObjectField(name);
                field.tooltip = tooltip;
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

        /// <summary>
        /// Evaluator Configuration Field that represents multiples tags.
        /// </summary>
        [Serializable]
        public class GroupedTagsField : EvaluatorConfigurationField
        {
            /// <summary>
            /// List of <see cref="MainTagField"/>s conforming a group.
            /// </summary>
            [SerializeField]
            List<MainTagField> tagsFields = new();
            
            /// <summary>
            /// List View for displaying tags.
            /// </summary>
            ListView list;

            /// <summary>
            /// Property for List View of tags. If null, initializes a new List View
            /// </summary>
            public ListView List
            {
                get
                {
                    if (list == null)
                        SetField();
                    return list;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="groupName">Identifier of the group field.</param>
            /// <param name="tagsNamesAndChars">Pairs of tags names and their respective characteristics.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public GroupedTagsField(string groupName, List<Tuple<string, LBSCharacteristic>> tagsNamesAndChars, string tooltip = "") : base(groupName, tooltip)
            {
                foreach(Tuple<string, LBSCharacteristic> nameAndChar in tagsNamesAndChars)
                    tagsFields.Add(new MainTagField(nameAndChar));

                SetField();
            }

            public override VisualElement GetField() => List;

            protected override void SetField()
            {
                list = new ListView();
                list.tooltip = tooltip;
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

        /// <summary>
        /// Evaluator Configuration Field that represents a single integer value.
        /// </summary>
        [Serializable]
        public class IntegerConfigurationField : EvaluatorConfigurationField
        {
            /// <summary>
            /// The current value of the field.
            /// </summary>
            [SerializeField]
            int value;

            /// <summary>
            /// Whether this value should be managed with a <see cref="SliderInt"/> or not.
            /// </summary>
            [SerializeField]
            bool useSlider;
            /// <summary>
            /// The minimum value accepted by the Slider.
            /// </summary>
            [SerializeField]
            int minValue;
            /// <summary>
            /// The maximum value accepted by the Slider.
            /// </summary>
            [SerializeField]
            int maxValue;

            /// <summary>
            /// Integer Field containing a numeric value. <br />
            /// It mantains <see cref="value"/> updated.
            /// </summary>
            IntegerField field;
            /// <summary>
            /// Integer type Slider containing a numeric value between a range.<br />
            /// It mantains <see cref="value"/> updated and restricts it between <see cref="minValue"/> and <see cref="maxValue"/>.
            /// </summary>
            SliderInt slider;

            /// <summary>
            /// Property for <see cref="field"/>. <br />
            /// If null, it initializes a new Integer Field.
            /// </summary>
            public IntegerField Field
            {
                get
                {
                    if (field is null)
                        SetField();
                    return field;
                }
            }

            /// <summary>
            /// Property for <see cref="slider"/>. <br />
            /// If null, it initializes a new Slider.
            /// </summary>
            public SliderInt Slider
            {
                get
                {
                    if(slider is null)
                        SetField();
                    return slider;
                }
            }

            /// <summary>
            /// <b>The standard constructor.</b> <br />
            /// Use this constructor to create an Integer Field that does <b>NOT</b> use a slider.
            /// </summary>
            /// <param name="fieldName">Identifier of the field.</param>
            /// <param name="value">Initial value of the field.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public IntegerConfigurationField(string fieldName, int value, string tooltip = "") : base(fieldName, tooltip)
            {
                this.value = value;

                SetField();
            }

            /// <summary>
            /// An alternative constructor. <br />
            /// Use this constructor to create an Integer Field that does use a slider.
            /// </summary>
            /// <param name="fieldName">Identifier of the field.</param>
            /// <param name="value">Initial value of the field.</param>
            /// <param name="minValue">The minimum value accepted by the slider.</param>
            /// <param name="maxValue">The maximum value accepted by the slider.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public IntegerConfigurationField(string fieldName, int value, int minValue, int maxValue, string tooltip = "") : base(fieldName, tooltip)
            {
                this.value = value;
                this.minValue = minValue;
                this.maxValue = maxValue;

                useSlider = true;

                SetField();
            }

            public override VisualElement GetField() => useSlider ? Slider : Field;

            protected override void SetField()
            {
                if (useSlider)
                {
                    slider = new SliderInt(name, minValue, maxValue);
                    slider.tooltip = tooltip;
                    slider.value = value;
                    slider.showInputField = true;
                    slider.RegisterValueChangedCallback(evt =>
                    {
                        if (slider.value < minValue)
                            slider.SetValueWithoutNotify(minValue);
                        else if (slider.value > maxValue)
                            slider.SetValueWithoutNotify(maxValue);

                        value = slider.value;
                    });
                }
                else
                {
                    field = new IntegerField(name);
                    field.tooltip = tooltip;
                    field.value = value;
                    field.RegisterValueChangedCallback(evt =>
                    {
                        value = field.value;
                    });
                }
            }

            public override object GetValue() => value;
        }

        /// <summary>
        /// Evaluator Configuration Field that represents a single floating-point value.
        /// </summary>
        [Serializable]
        public class FloatConfigurationField : EvaluatorConfigurationField
        {
            /// <summary>
            /// The current value of the field.
            /// </summary>
            [SerializeField]
            float value;

            /// <summary>
            /// Whether this value should be managed with a <see cref="SliderInt"/> or not.
            /// </summary>
            [SerializeField]
            bool useSlider;
            /// <summary>
            /// The minimum value accepted by the Slider.
            /// </summary>
            [SerializeField]
            float minValue;
            /// <summary>
            /// The maximum value accepted by the Slider.
            /// </summary>
            [SerializeField]
            float maxValue;

            /// <summary>
            /// Float Field containing a numeric value. <br />
            /// It mantains <see cref="value"/> updated.
            /// </summary>
            FloatField field;
            /// <summary>
            /// Float type Slider containing a numeric value between a range.<br />
            /// It mantains <see cref="value"/> updated and restricts it between <see cref="minValue"/> and <see cref="maxValue"/>.
            /// </summary>
            Slider slider;

            /// <summary>
            /// Property for <see cref="field"/>. <br />
            /// If null, it initializes a new Float Field.
            /// </summary>
            public FloatField Field
            {
                get
                {
                    if (field is null)
                        SetField();
                    return field;
                }
            }

            /// <summary>
            /// Property for <see cref="slider"/>. <br />
            /// If null, it initializes a new Slider.
            /// </summary>
            public Slider Slider
            {
                get
                {
                    if (slider is null)
                        SetField();
                    return slider;
                }
            }

            /// <summary>
            /// <b>The standard constructor.</b> <br />
            /// Use this constructor to create a Float Field that does <b>NOT</b> use a slider.
            /// </summary>
            /// <param name="fieldName">Identifier of the field.</param>
            /// <param name="value">Initial value of the field.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public FloatConfigurationField(string fieldName, float value, string tooltip = "") : base(fieldName, tooltip)
            {
                this.value = value;

                SetField();
            }

            /// <summary>
            /// An alternative constructor. <br />
            /// Use this constructor to create an Integer Field that does use a slider.
            /// </summary>
            /// <param name="fieldName">Identifier of the field.</param>
            /// <param name="value">Initial value of the field.</param>
            /// <param name="minValue">The minimum value accepted by the slider.</param>
            /// <param name="maxValue">The maximum value accepted by the slider.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public FloatConfigurationField(string fieldName, float value, float minValue, float maxValue, string tooltip = "") : base(fieldName, tooltip)
            {
                this.value = value;
                this.minValue = minValue;
                this.maxValue = maxValue;

                useSlider = true;

                SetField();
            }

            public override VisualElement GetField() => useSlider ? Slider : Field;

            protected override void SetField()
            {
                if (useSlider)
                {
                    slider = new Slider(name, minValue, maxValue);
                    slider.tooltip = tooltip;
                    slider.value = value;
                    slider.showInputField = true;
                    slider.RegisterValueChangedCallback(evt =>
                    {
                        if (slider.value < minValue)
                            slider.SetValueWithoutNotify(minValue);
                        else if (slider.value > maxValue)
                            slider.SetValueWithoutNotify(maxValue);

                        value = slider.value;
                    });
                }
                else
                {
                    field = new FloatField(name);
                    field.tooltip = tooltip;
                    field.value = value;
                    field.RegisterValueChangedCallback(evt =>
                    {
                        value = field.value;
                    });
                }
            }

            public override object GetValue() => value;
        }

        /// <summary>
        /// Evaluator Configuration Field that represents a range composed of two values.
        /// </summary>
        [Serializable]
        public class MinMaxConfigurationField : EvaluatorConfigurationField
        {
            /// <summary>
            /// The current pair of values of the field as a Vector2.
            /// </summary>
            [SerializeField]
            Vector2 value;

            /// <summary>
            /// The lowest value accepted by the slider.
            /// </summary>
            [SerializeField]
            float lowLimit;
            /// <summary>
            /// The highest value accepted by the slider.
            /// </summary>
            [SerializeField]
            float highLimit;

            /// <summary>
            /// Slider containing two numeric values between a wider range<br />
            /// It mantains <see cref="value"/> updated and restricts it between <see cref="lowLimit"/> and <see cref="highLimit"/>.
            /// </summary>
            MinMaxSlider slider;

            /// <summary>
            /// The current minimum value of the field
            /// </summary>
            public float Min
            {
                get => value.x;
                set => this.value.x = value;
            }

            /// <summary>
            /// The current maximum value of the field
            /// </summary>
            public float Max
            {
                get => value.y;
                set => this.value.y = value;
            }

            /// <summary>
            /// Property for <see cref="slider"/>. <br />
            /// If null, it initializes a new Slider.
            /// </summary>
            public MinMaxSlider Slider
            {
                get
                {
                    if (slider is null)
                        SetField();
                    return slider;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="fieldName">Identifier of the field.</param>
            /// <param name="minValue">Initial minimum value of the field.</param>
            /// <param name="maxValue">Initial maximum value of the field.</param>
            /// <param name="lowLimit">The lowest value accepted by the slider.</param>
            /// <param name="highLimit">The highest value accepted by the slider.</param>
            /// <param name="tooltip">Short description of the field's purpose.</param>
            public MinMaxConfigurationField(string fieldName, float minValue, float maxValue, float lowLimit, float highLimit, string tooltip = "") : base(fieldName, tooltip)
            {
                Min = minValue;
                Max = maxValue;
                this.lowLimit = lowLimit;
                this.highLimit = highLimit;

                SetField();
            }

            public override VisualElement GetField() => Slider;

            protected override void SetField()
            {
                slider = new MinMaxSlider(name, Min, Max, lowLimit, highLimit);
                slider.tooltip = tooltip;
                slider.RegisterValueChangedCallback(evt =>
                {
                    value = evt.newValue;
                });
            }

            public override object GetValue() => value;
        }

        /// <summary>
        /// Instance of the evaluator to which the configuration belongs.
        /// </summary>
        [SerializeReference]
        public object target;
        /// <summary>
        /// Every Field of this configuration.
        /// </summary>
        [SerializeReference]
        public List<EvaluatorConfigurationField> fields = new();

        /// <summary>
        /// Creates a new Evaluator Configuration asset for an evaluator, or updates an existent one.
        /// </summary>
        /// <param name="config">The evaluator's configuration.</param>
        /// <param name="type">The type of the evaluator.</param>
        /// <param name="getFields">Method that retrieves new Configuration Fields for the evaluator.</param>
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
                config = CreateInstance<EvaluatorConfiguration>();
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

        /// <summary>
        /// Generic getter of a single Configuration Field value.
        /// </summary>
        /// <typeparam name="T">The type of the value to get.</typeparam>
        /// <param name="name">The identifier of the field.</param>
        /// <returns>The value of type T contained in a field named as name.</returns>
        public T GetValue<T>(string name)
        {
            return (T)fields.Find(f => f.name.Equals(name)).GetValue();
        }

        /// <summary>
        /// Generic getter of a collection of Configuration Field values.
        /// </summary>
        /// <typeparam name="T">The type of each element of the collection to get.</typeparam>
        /// <param name="name">The identifier of the collection.</param>
        /// <returns>The collection of type T contained in a field named as name.</returns>
        public IEnumerable<T> GetValues<T>(string name)
        {
            object val = fields.Find(f => f.name.Equals(name)).GetValue();
            var t = val.GetType();

            var ret = (val as IEnumerable<object>).Select(v => (T)v);
            return ret;
        }
    }
}
