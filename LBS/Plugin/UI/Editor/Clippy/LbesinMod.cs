using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.AI.Clippy
{
    [CreateAssetMenu(fileName = "LbesinMod", menuName = "Scriptable Objects/LbesinMod")]
    public class LbesinMod : ScriptableObject
    {
        public string Id;
        public int SortingIndex;
        public Color Color;
        public VectorImage Icon;

        public Button GenerateButton()
        {
            var button = new Button();

            button.style.position = Position.Absolute;

            button.style.width = 25;
            button.style.height = 25;

            button.style.backgroundImage = new StyleBackground(Icon);

            //Debug.Log($"[LbesinMod]: Color: {Color}");
            button.style.unityBackgroundImageTintColor = Color;
            button.style.backgroundColor = UnityEngine.Color.clear;

            button.style.borderBottomWidth = 0;
            button.style.borderTopWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;

            return button;
        }
    }
}
