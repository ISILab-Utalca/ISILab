using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace ISILab.LBS.Plugin.UI.Editor.CustomComponents.ComplexComponents
{
    
    
    [UxmlElement]
    public partial class LBSCustomRandomSeedField: LBSCustomIntSlider
    {
        private static readonly string[]  ICON_GUID =
        {
            "b0c5b22738f6c7849affc75488e31da0", //1
            "a789a3e69cb259c469f4c91a1a90ccdf", //2
            "ad7a9ae2ddfe4154e8f2ad732c2c60ef", //3
            "5c3d5921d699eb14494e61440809dcbb", //4
            "c1bed1e39de970e489f0c9633444dcca", //5
            "1f9e7d5f8fa0fb24585986994cdc6864", //6
        };

        private Button m_diceButton;

        public LBSCustomRandomSeedField()
        {
            AddToClassList("lbs-ramdom-int-field");
            
            m_diceButton = new Button();
            this.Add(m_diceButton);
            m_diceButton.SendToBack();
            m_diceButton.style.backgroundImage = new StyleBackground(Macros.LBSAssetMacro.LoadAssetByGuid<VectorImage>(ICON_GUID[0]));
            m_diceButton.RegisterCallback<ClickEvent>(_evt => GenerateRandomSeed());
            //m_diceButton.BringToFront(); // The opposite to SendToBack
            this.highValue = Int32.MaxValue;
        }


        public void GenerateRandomSeed()
        {
            
        }
    }
}
