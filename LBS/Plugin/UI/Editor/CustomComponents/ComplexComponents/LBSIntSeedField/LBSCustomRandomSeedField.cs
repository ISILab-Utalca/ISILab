using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Random = UnityEngine.Random;

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
        private TextField m_inputField;

        public LBSCustomRandomSeedField()
        {
            AddToClassList("lbs-random-int-field");
            
            m_diceButton = new Button();
            m_diceButton.AddToClassList("lbs-icon");
            this.Add(m_diceButton);
            m_diceButton.SendToBack();
            m_diceButton.style.backgroundImage = new StyleBackground(Macros.LBSAssetMacro.LoadAssetByGuid<VectorImage>(ICON_GUID[0]));
            m_diceButton.RegisterCallback<ClickEvent>(_evt => GenerateRandomSeed());
            //m_diceButton.BringToFront(); // The opposite to SendToBack
            this.highValue = Int32.MaxValue;

        }


        public void GenerateRandomSeed()
        {
            int seed = Random.Range(0, Int32.MaxValue);
            value = seed;
            RandomizeIcon();
            this.MarkDirtyRepaint();
        }

        private void RandomizeIcon()
        {
            int selected = value % 6;
            if (m_diceButton != null)
            {
                VectorImage newIcon  = Macros.LBSAssetMacro.LoadAssetByGuid<VectorImage>(ICON_GUID[selected]);
                m_diceButton.style.backgroundImage = new StyleBackground(newIcon);
            }
        }
    }
}
