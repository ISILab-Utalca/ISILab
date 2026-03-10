using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomToggle : UnityEngine.UIElements.Toggle
    {
        //string styles so they can be added later

        //private VisualElement m_Input;

        public LBSCustomToggle() : base()
        {
            //addToClassList para agregar los estilos necesarios

            //m_Input = this.Q(className: BaseField<bool>.inputUssClassName);
            //
            RemoveFromClassList("unity-toggle");
            AddToClassList("lbs-custom-toggle");

            /*
            RegisterCallback<ClickEvent>(_evt => OnClick(_evt));
            RegisterCallback<KeyDownEvent>(evt => OnKeydownEvent(evt));
            RegisterCallback<NavigationSubmitEvent>(evt => OnSubmit(evt));
            */
        }

    }
}