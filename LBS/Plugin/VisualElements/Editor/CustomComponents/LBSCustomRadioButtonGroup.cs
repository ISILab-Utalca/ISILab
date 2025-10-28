using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomRadioButtonGroup: RadioButtonGroup 
    {
        public LBSCustomRadioButtonGroup() : base()
        {
            AddToClassList("lbs-custom-radio-button-group");
        }


        public LBSCustomRadioButtonGroup(String _label, List<String> _options) : this()
        {
            choices = _options;
            label = _label;
        }
    }
    
}

