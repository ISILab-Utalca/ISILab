using ISILab.Commons.Utility.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.CustomComponents
{
    public interface ILBSComplexComponent
    {
        public abstract VisualTreeAsset GetVisualTreeForThis();
    }
}
