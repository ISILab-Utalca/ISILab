using System;
using System.ComponentModel;
using ISILab.Commons.Utility.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.CustomComponents
{
    public interface ILBSComplexComponent
    {
        public abstract VisualTreeAsset GetVisualTreeForThis();
    }
    
    [AttributeUsage(AttributeTargets.Class,  AllowMultiple = false, Inherited = true)]
    public class LBSComplexElementAttribute: Attribute {

        public static VisualTreeAsset GetVisualTreeForThis(VisualElement _element)
        {
            VisualTreeAsset vta = DirectoryTools.GetAssetByName<VisualTreeAsset>(_element.GetType().Name);
            if (vta == null)
            {
                throw new WarningException("No VisualTreeAsset found");
            }
            vta?.CloneTree(_element);
            return vta;
        }
    }
    
    
}
