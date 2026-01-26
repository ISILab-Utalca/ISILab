using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomEnumFlagField: EnumFlagsField
    {
        public LBSCustomEnumFlagField() : base()
        {
            AddToClassList("lbs-enum-flag-field");
        }
    }
}
