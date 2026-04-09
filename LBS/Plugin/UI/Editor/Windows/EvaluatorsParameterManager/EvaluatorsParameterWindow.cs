using ISILab.Commons.Utility.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EvaluatorsParameterWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Window/UI Toolkit/EvaluatorsParameterWindow")]
    public static void ShowExample()
    {
        EvaluatorsParameterWindow wnd = GetWindow<EvaluatorsParameterWindow>();
        wnd.titleContent = new GUIContent("EvaluatorsParameterWindow");
    }

    public void CreateGUI()
    {
        // 1. Clonar el UXML
        var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("EvaluatorsParameterWindow");
        if (visualTree != null)
            visualTree.CloneTree(rootVisualElement);

        // 2. CARGAR EL ESTILO (Aquí está la magia que te falta)
        // Busca el archivo .uss por nombre. Asegúrate que el nombre coincida con tu archivo de estilos.
        var styleSheet = DirectoryTools.GetAssetByName<StyleSheet>("LBSMainTheme");

        if (styleSheet != null)
        {
            rootVisualElement.styleSheets.Add(styleSheet);
        }
        
        /*
        else
        {
            // Si el estilo de la pestaña anterior es el que quieres, búscalo por su nombre original
            var originalStyle = DirectoryTools.GetAssetByName<StyleSheet>("PopulationAssistantWindow");
            if (originalStyle != null)
                rootVisualElement.styleSheets.Add(originalStyle);
        }
        */
    }
}
