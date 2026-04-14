using ISILab.Commons.Utility.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static ISILab.LBS.Editor.PopulationAssistantTab;

public class EvaluatorsParameterWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    private EvaluatorData evData;

    /* testing if can be removed, currently yes
    [MenuItem("Window/UI Toolkit/EvaluatorsParameterWindow")]
    public static void ShowExample()
    {
        EvaluatorsParameterWindow wnd = GetWindow<EvaluatorsParameterWindow>();
        wnd.titleContent = new GUIContent("EvaluatorsParameterWindow");
    }
    */
    public void InitData(EvaluatorData evData)
    {
        this.evData = evData;
        RefreshData();
    }
    public void CreateGUI()
    {
        // 1. Clonar el UXML
        m_VisualTreeAsset = DirectoryTools.GetAssetByName<VisualTreeAsset>("EvaluatorsParameterWindow");
        if (m_VisualTreeAsset != null)
            m_VisualTreeAsset.CloneTree(rootVisualElement);

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
        RefreshData();
    }
    public void RefreshData()
    {
        if (rootVisualElement == null) return;
        
        //UI refresh logic
    }
}
