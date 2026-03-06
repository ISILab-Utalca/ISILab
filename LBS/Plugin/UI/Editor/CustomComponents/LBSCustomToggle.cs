using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.CustomComponents
{
    [UxmlElement]
    public partial class LBSCustomToggle : BaseField<bool>
    {
        //string styles so they can be added later

        private VisualElement m_Input;
        private Toggle toggle;

        public LBSCustomToggle(string label) : base(label, null)
        {
            RemoveFromClassList(ussClassName);
            //addToClassList para agregar los estilos necesarios

            m_Input = this.Q(className: BaseField<bool>.inputUssClassName);



        }


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}