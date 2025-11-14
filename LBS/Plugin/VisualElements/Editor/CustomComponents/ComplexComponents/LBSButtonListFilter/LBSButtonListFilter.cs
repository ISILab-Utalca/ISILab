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

/// <summary>
/// A window that shows a list of buttons representing bundles (or any list of objects), with a search bar to filter them.
/// </summary>
public class LBSButtonListFilter : EditorWindow
{
    #region VIEW ELEMENTS

    private LBSCustomListView buttonListView;
    private LBSCustomTextField searchField;
    private LBSCustomButton closeButton;

    #endregion

    private List<Bundle> _bundles;
    private List<Bundle> _filteredBundles;
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
        win.titleContent = new GUIContent("Select Bundle");

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

        _filteredBundles = _bundles;

        buttonListView.itemsSource = _filteredBundles;
        buttonListView.makeItem = () => new LBSCustomButton();
        buttonListView.bindItem = (element, index) =>
        {
            if (element is not LBSCustomButton button) return;

            if (buttonListView.itemsSource is not List<Bundle> source || 
                index < 0 || index >= source.Count) return;

            var bundle = source[index];

            button.iconImage = (Background)EditorGUIUtility.ObjectContent(bundle, typeof(Bundle)).image;
            button.text = bundle != null ? bundle.name : string.Empty;

            button.Q<Image>().style.maxWidth = 20;

            if (button.userData is Action oldCb)
                button.clicked -= oldCb;

            Action onClick = () =>
            {
                _onPick?.Invoke(bundle);
                Close();
                GUIUtility.ExitGUI();
            };

            button.userData = onClick;
            button.clicked += onClick;
        };

        searchField.RegisterValueChangedCallback(evt =>
        {
            _search = evt.newValue;
            ApplyFilter();
        });

        closeButton.clicked += () =>
        {
            Close();
            GUIUtility.ExitGUI();
        };
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(_search))
        {
            _filteredBundles = _bundles;
        }
        else
        {
            var term = _search.Trim();
            _filteredBundles = _bundles
                .Where(b =>
                    b != null &&
                    !string.IsNullOrEmpty(b.name) &&
                    b.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        buttonListView.itemsSource = _filteredBundles;
        buttonListView.Rebuild();
    }
}
