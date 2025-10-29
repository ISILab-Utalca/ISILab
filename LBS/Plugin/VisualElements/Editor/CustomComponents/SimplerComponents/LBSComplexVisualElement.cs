using System.ComponentModel;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.CustomComponents;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    public class LBSComplexVisualElement: VisualElement, ILBSComplexComponent, ILBSField 
    {
        public LBSComplexVisualElement()
        {
            AddToClassList("lbs-component");
        }
        
        public VisualTreeAsset GetVisualTreeForThis()
        {
            VisualTreeAsset vta = DirectoryTools.GetAssetByName<VisualTreeAsset>(GetType().Name);
            Debug.Log(GetType().Name);
            if (vta == null)
            {
                throw new WarningException("No VisualTreeAsset found");
            }
            vta?.CloneTree(this);
            return vta;
        }
    }
}

