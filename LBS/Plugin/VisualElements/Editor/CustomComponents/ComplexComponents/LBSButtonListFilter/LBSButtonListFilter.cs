using ISILab.Commons.Utility.Editor;
using ISILab.LBS;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Internal;
using ISILab.LBS.VisualElements;
using LBS.Bundles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class LBSButtonListFilter : EditorWindow
{
    //Cada botón debe tener un 0.05 de grow

    #region VIEW ELEMENTS

    private LBSCustomListView buttonListView;
    private LBSCustomTextField searchField;
    private LBSCustomButton closeButton;

    #endregion

    private List<Bundle> _bundles;
    private Action<Bundle> _onPick;
    private string _search = "";
    private Vector2 _scroll;

    private void CreateGUI()
    {
        var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("LBSButtonListFilter");
        visualTree.CloneTree(rootVisualElement);

        buttonListView = rootVisualElement.Q<LBSCustomListView>("ButtonListView");
        searchField = rootVisualElement.Q<LBSCustomTextField>("SearchBar");
        closeButton = rootVisualElement.Q<LBSCustomButton>("CloseButton");

        Init();
    }

    public static void Show(List<Bundle> bundles, Action<Bundle> onPick)
    {
        var win = CreateInstance<LBSButtonListFilter>();
        win._bundles = bundles ?? new List<Bundle>();
        win._onPick = onPick;
        win.titleContent = new GUIContent("Seleccionar");

        win.ShowUtility();
    }

    private void TestList()
    {
        Bundle test = new Bundle();
        test.name = "Test Bundle";

        for (int i = 0; i < 50; i++)
        {
            _bundles.Add(test);
        }
    }

    private void Init()
    {
        //TestList();

        buttonListView.itemsSource = _bundles;
        buttonListView.makeItem = () => new LBSCustomButton();
        buttonListView.bindItem = (element, index) =>
        {
            var button = element as LBSCustomButton;
            if (button != null)
            {
                var bundle = _bundles[index];
                button.text = bundle.name;
                button.iconImage = Background.FromVectorImage(bundle.Icon);
                button.clicked += () =>
                {
                    _onPick?.Invoke(bundle);
                    Close();
                    GUIUtility.ExitGUI();
                };
            }
        };

        searchField.RegisterValueChangedCallback(evt =>
        {
            _search = evt.newValue;
            buttonListView.Rebuild();
        });

        closeButton.clicked += () =>
        {
            Close();
            GUIUtility.ExitGUI();
        };
    }

    /*
    private void OnGUI()
    {
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Buscar:", GUILayout.Width(60));
            _search = EditorGUILayout.TextField(_search);
        }

        EditorGUILayout.Space();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        var query = string.IsNullOrWhiteSpace(_search)
            ? _bundles
            : _bundles.Where(b => b.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        foreach (var b in query)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(EditorGUIUtility.ObjectContent(b, typeof(Bundle)), GUILayout.Height(22)))
                {
                    _onPick?.Invoke(b);
                    Close();
                    GUIUtility.ExitGUI();
                }
            }
        }

        EditorGUILayout.EndScrollView();

        if (query.Count == 0)
        {
            EditorGUILayout.HelpBox("No hay resultados para el filtro actual.", MessageType.Info);
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cerrar", GUILayout.Width(80)))
            {
                Close();
                GUIUtility.ExitGUI();
            }
        }
    }
    */

}
