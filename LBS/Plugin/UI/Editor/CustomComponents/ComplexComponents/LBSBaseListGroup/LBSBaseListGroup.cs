using ISILab.LBS.CustomComponents;
using UnityEditor.VersionControl;using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.Editor.UI.CustomComponents
{
    [UxmlElement]
    public partial class LBSBaseListGroup : LBSComplexVisualElement
    {
        public LBSBaseListGroup() : base()
        {
            GetVisualTreeForThis();
        }
    }
}

