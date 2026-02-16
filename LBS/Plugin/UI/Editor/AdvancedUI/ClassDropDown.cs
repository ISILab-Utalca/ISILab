using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine.UIElements;
using ISILab.Commons.Utility;
using ISILab.LBS.CustomComponents;
using System.Reflection;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class ClassDropDown : LBSCustomDropdown
    {
   //     public new class UxmlFactory : UxmlFactory<ClassDropDown, UxmlTraits> {}

        private readonly UxmlStringAttributeDescription m_Label = new UxmlStringAttributeDescription { name = "Label", defaultValue = "Class DropDown" };
        
        //public void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        //{
        //    UnityEngine.Debug.Log("Class Dropdown Init");
        //    //Init(ve, bag, cc); // QUE?
        //
        //    ClassDropDown field = (ClassDropDown)ve;
        //    field.Label = m_Label.GetValueFromBag(bag, cc);
        //    
        //}

        #region FIELDS
        //Label label;

        Type type;

        bool filterAbstract;
        protected List<Type> types;

        #endregion

        public string Value
        {
            get => value;
            set
            {
                if (choices.Contains(value))
                    this.value = value;
            }
        }

        //public string Label
        //{
        //    get => label?.text;
        //    set 
        //    {
        //        var notify = (INotifyValueChanged<string>)label;
        //        UnityEngine.Assertions.Assert.IsNotNull(notify, "Cast fallo");
        //        notify.SetValueWithoutNotify(value);
        //    }
        //}

        public Type TypeValue => types[choices.IndexOf(value)];

        public Type Type
        {
            get => type;
            set
            {
                type = value;
                UpdateOptions();
            }
        }

        public bool FilterAbstract
        {
            get => filterAbstract;
            set
            {
                filterAbstract = value;
                UpdateOptions();
            }
        }

        public ClassDropDown() : base()
        {
            
        }

        public void Init()
        {
            //label = this.Q<Label>();
            //UnityEngine.Assertions.Assert.IsNotNull(label, "No se encontro el visual element");
            //Label = "Class DropDown";

            label = "Class DropDown";
            this.SetValueWithoutNotify("");
        }

        public virtual void UpdateOptions()
        {
            choices.Clear();

            IEnumerable<Type> types = null;

            if (Type.IsClass)
            {
                types = Reflection.GetAllSubClassOf(Type);
            }
            else if (Type.IsInterface)
            {
                types = Reflection.GetAllImplementationsOf(Type);
            }

            types = types.Where(t => t.GetCustomAttribute(typeof(ObsoleteAttribute), false) is null);

            if (filterAbstract)
            {
                types = types.Where(t => !t.IsAbstract);
            }

            var options = types.Select(t => t.Name).ToList();

            this.types = types.ToList();
            choices = options;
        }

        public object GetChoiceInstance()
        {
            object obj = null;
            var dv = value;
            var dx = choices.IndexOf(dv);
            
            if (dx < 0)
                return null;
            
            var t = types[dx];
            try
            {
                obj = Activator.CreateInstance(t);
            }
            catch
            {
                throw new FormatException(t + " class needs to have an empty constructor.");
            }
            
            return obj;
        }
    }
}