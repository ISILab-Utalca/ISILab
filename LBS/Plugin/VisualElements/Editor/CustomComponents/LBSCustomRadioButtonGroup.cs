using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomRadioButtonGroup: RadioButtonGroup
    {

        private int selectedChoices = 0;

        [UxmlAttribute]
        public int SelectedChoice
        {
            get => selectedChoices;
            set
            {
                selectedChoices = value;
                if (choices != null && selectedChoices < choices.Count() && selectedChoices >= 0)
                {
                    SelectChoice(value);
                }
                else
                {
                    Debug.LogWarning("Selected choice " + selectedChoices + " is out of range.");
                }
            }
        }
        
        
        
        public LBSCustomRadioButtonGroup() : base()
        {
            AddToClassList("lbs-custom-radio-button-group");
        }


        public LBSCustomRadioButtonGroup(String _label, List<String> _options) : base(_label, _options)
        {
            AddToClassList("lbs-custom-radio-button-group");
            //choices = _options;
            //label = _label;
        }


        public void SelectChoice(int _index)
        {
            var choice = choices.ElementAt(_index);
            var radiobuttons = this.Query<RadioButton>().ToList();
            radiobuttons[_index].SetValueWithoutNotify(true);
        }
    }
    
}

