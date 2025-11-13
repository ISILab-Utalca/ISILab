using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.VisualElements.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainConnectionGridEditorWindow : EditorWindow
{
    public LBSTerrainConnectionGrid connectionGridTarget;

    //Tracking thingies
    public int currentColor;
    enum GridTerrainTool { Brush, Fill, Eraser };
    GridTerrainTool activeTool;

    //Color Buttons
    public LBSSelectableButton baseColorButton;
    public List<LBSSelectableButton> colorButtons = new List<LBSSelectableButton>();
    public LBSCustomButton addColorButton;
    public VisualElement colorList;

    //Tools
    public List<LBSToolbarToggle> gridTerrainTools = new List<LBSToolbarToggle>();
    
    public LBSToolbarToggle brushTool;
    public LBSToolbarToggle fillTool;
    public LBSToolbarToggle eraserTool;


    public Dictionary<int, UnityEngine.Color> ColorPalette => connectionGridTarget.FlagColorPalette;

    public void CreateGUI()
    {
        var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TerrainConnectionGridEditorWindow");
        visualTree.CloneTree(rootVisualElement);

        //Colors!
        colorList = rootVisualElement.Q<VisualElement>("ColorListElemn");
        addColorButton = rootVisualElement.Q<LBSCustomButton>("AddColorButton");
        addColorButton.RegisterCallback<ClickEvent>((evt) => { AddColorKey(); });
        UpdateColorButtons();

        //Tools!
        brushTool = rootVisualElement.Q<LBSToolbarToggle>("BrushTool");
        //brushTool.RegisterCallback<ClickEvent>((evt) => { SwitchTools(GridTerrainTool.Brush); });
        

        fillTool = rootVisualElement.Q<LBSToolbarToggle>("FillTool");
        //brushTool.RegisterCallback<ClickEvent>((evt) => { SwitchTools(GridTerrainTool.Fill); });
        eraserTool = rootVisualElement.Q<LBSToolbarToggle>("EraserTool");

        //brushTool.RegisterCallback<ClickEvent>((evt) => { SwitchTools(GridTerrainTool.Eraser); });

        gridTerrainTools.Add(brushTool);
        gridTerrainTools.Add(fillTool);
        gridTerrainTools.Add(eraserTool);

    }
    void SwitchTools(GridTerrainTool _newTool)
    {

        activeTool = _newTool;
    }

    public void AddColorKey()
    {
        int _counter = ColorPalette.Count + 1;
        Color _color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.3f, 1f);
        foreach (KeyValuePair<int, Color> item in ColorPalette)
        {
            while(item.Value == _color) Random.ColorHSV(0f, 1f, 1f, 1f, 0.3f, 1f);
            if (item.Key == _counter) _counter++;
        }
        ColorPalette.Add(_counter, _color);
        AddColorButton(_counter, _color);
    }

    public void RemoveColorKey(int key)
    {
        if (ColorPalette[key]!=null)
        {
            ColorPalette.Remove(key);
        }
    }

    public void AddColorButton(int key, Color color)
    {
        //Add button and store its key as data
        var newButton = new LBSSelectableButton(color);
        newButton.Data = key;

        //Add button functionality
        newButton.OnExecute += () => { 
            foreach(LBSSelectableButton button in colorButtons) { button.ToggleButtonSelected(false); }
            newButton.ToggleButtonSelected(true);
            currentColor = newButton.Data;
            Debug.Log("current color: " + currentColor);
        };
        //this is meant to pick the color from colorButtons btw!
        newButton.OnRemove += () => { colorButtons.Remove(newButton); RemoveColorKey(newButton.Data); };

        //Add to the visual window...
        colorList.Add(newButton);
        //...And to the button list
        colorButtons.Add(newButton);
    }

    public void UpdateColorButtons()
    {
        colorList.Clear();
        colorButtons.Clear();

        foreach (KeyValuePair<int, Color> item in ColorPalette)
        {
            AddColorButton(item.Key, item.Value);
        }
    }
}
