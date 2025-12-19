using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.VisualElements.Editor;
using LBS.Bundles;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class TerrainConnectionGridEditorWindow : EditorWindow
{
    #region FIELDS
    public LBSTerrainConnectionGrid connectionGridTarget;

    //Tracking thingies
    public int currentColor;
    public enum GridTerrainTool { Brush, Fill, Eraser };
    private GridTerrainTool activeTool;

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

    //Grids
    public VisualElement gridsVE;

    //Zoom
    private Slider previewScaleSlider;
    private LBSCustomUnsignedIntegerField zoomScaleInt;
    public float fovScale;

    //Buttons
    private Button clearButton;
    private Button revertButton;
    private Button saveButton;

    private List<AssetGridEditorWindow> editorWindows;

    #endregion

    #region PROPERTIES
    public Dictionary<int, Color> ColorPaletteKey
    {
        get
        {
            var dict = new Dictionary<int, Color>();
            for(int i=0; i<connectionGridTarget.ColorPalette.Count; i++)
            {
                dict.Add(connectionGridTarget.ColorPaletteID[i], connectionGridTarget.ColorPalette[i]);
            }
            return dict;
        }
    }
    public GridTerrainTool ActiveTool => activeTool;
    #endregion

    public Action SetFOVScale;
    public Action OnWindowClosed;
    public Action OnColorRemoved;
    public Action<float> OnScaleModify;

    #region CONSTRUCTOR
    public void CreateGUI()
    {
        //Initialize connection grid if not initialized
        if(connectionGridTarget.GridList == null || connectionGridTarget.GridList.Count!=connectionGridTarget.Assets.Count)
        {
            connectionGridTarget.UpdateGridList();
        }

        var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("TerrainConnectionGridEditorWindow");
        visualTree.CloneTree(rootVisualElement);

        //Colors!
        colorList = rootVisualElement.Q<VisualElement>("ColorListElemn");
        addColorButton = rootVisualElement.Q<LBSCustomButton>("AddColorButton");
        addColorButton.RegisterCallback<ClickEvent>((evt) => { AddColorKey(); });

        //If the palette is empty I'll add a red button as a default. I think that makes things easier
        if(ColorPaletteKey.Count == 0)
        {
            connectionGridTarget.AddColor(1, Color.red);
        }
        UpdateColorButtons();

        colorButtons[0].OnExecute?.Invoke();

        //Tools!
        brushTool = rootVisualElement.Q<LBSToolbarToggle>("BrushTool");
        brushTool.RegisterValueChangedCallback((evt) => { SwitchTools(brushTool, GridTerrainTool.Brush); });
        brushTool.value = true;
        fillTool = rootVisualElement.Q<LBSToolbarToggle>("FillTool");
        fillTool.RegisterValueChangedCallback((evt) => { SwitchTools(fillTool, GridTerrainTool.Fill); });
        eraserTool = rootVisualElement.Q<LBSToolbarToggle>("EraserTool");
        eraserTool.RegisterValueChangedCallback((evt) => { SwitchTools(eraserTool, GridTerrainTool.Eraser); });
        gridTerrainTools.Add(brushTool);
        gridTerrainTools.Add(fillTool);
        gridTerrainTools.Add(eraserTool);

        //Zooming stuff!
        previewScaleSlider = rootVisualElement.Q<Slider>("PreviewScaleSlider");
        previewScaleSlider.RegisterValueChangedCallback((evt)=> { OnScaleModify?.Invoke(evt.newValue);});

        zoomScaleInt = rootVisualElement.Q<LBSCustomUnsignedIntegerField>("ZoomScaleInt");
        fovScale = 1 + (zoomScaleInt.value * 0.1f);
        zoomScaleInt.RegisterValueChangedCallback((evt) => {
            if (evt.newValue != evt.previousValue)
            {
                fovScale = 1 + (evt.newValue * 0.1f);
                SetFOVScale?.Invoke();
            }
        });

        //Revert button!
        revertButton = rootVisualElement.Q<Button>("RevertButton");
        //Clear button!
        clearButton = rootVisualElement.Q<Button>("ClearButton");
        //Save button!
        saveButton = rootVisualElement.Q<Button>("SaveButton");

        //Icons!
        editorWindows = new List<AssetGridEditorWindow>();
        gridsVE = rootVisualElement.Q<VisualElement>("GridsVE");

        foreach (AssetConnectionGrid _grid in connectionGridTarget.GridList)
        {
            //Debug.Log(_grid.AssetReference.obj);
            var _newGridWindow = new AssetGridEditorWindow(_grid, this);
            SetFOVScale += _newGridWindow.UpdateFOVScale;
            OnScaleModify += (newValue) => {
                //_newGridWindow.style.scale = new Scale(new Vector2(newValue, newValue));
                _newGridWindow.style.height = newValue * 128; 
                _newGridWindow.style.width = newValue * 128;
                _newGridWindow.MarkDirtyRepaint();
            };

            //Button interaction
            clearButton.clicked += () => { _newGridWindow.ClearGrid(); };
            saveButton.clicked += () => { _newGridWindow.SaveChanges(); };
            revertButton.clicked += () => { _newGridWindow.RevertChanges(); };
            //128 * (previewScaleSlider.value);
            OnWindowClosed += () => { _newGridWindow.OnRemove?.Invoke(); };
            gridsVE.Add(_newGridWindow);
            
        }

        saveButton.clicked += () => {
            EditorUtility.SetDirty(connectionGridTarget.Owner);
            AssetDatabase.SaveAssets();
        };

        //Because otherwise everything breaks lol
        OnScaleModify?.Invoke(previewScaleSlider.value);
    }

    private void OnDisable()
    {
        OnWindowClosed?.Invoke();
    }
    #endregion

    #region METHODS
    void SwitchTools(LBSToolbarToggle button, GridTerrainTool _newTool)
    {
        foreach(LBSToolbarToggle otherButton in gridTerrainTools)
        {
            if(otherButton!=button)
            {
                otherButton.SetValueWithoutNotify(false);
            }
        }
        activeTool = _newTool;
    }

    public void AddColorKey()
    {
        int _counter = ColorPaletteKey.Count + 1;
        Color _color = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.3f, 1f);
        foreach (KeyValuePair<int, Color> item in ColorPaletteKey)
        {
            while (item.Value == _color) UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.3f, 1f);
            if (item.Key == _counter) _counter++;
        }

        connectionGridTarget.AddColor(_counter, _color);
        AddColorButton(_counter, _color);
    }

    public void RemoveColorKey(int key)
    {
        if (ColorPaletteKey[key]!=null)
        {
            Debug.Log("removing color with ID " + key);
            connectionGridTarget.RemoveColor(key);
            OnColorRemoved?.Invoke();
            return;
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
        };
        //this is meant to pick the color from colorButtons btw!
        newButton.OnRemove += () => { colorButtons.Remove(newButton); RemoveColorKey(newButton.Data); };

        //Add to the visual window...
        colorList.Add(newButton);
        //...And to the button list
        colorButtons.Add(newButton);

        //Also let's select it, because why not!
        newButton.OnExecute?.Invoke();
    }

    public void UpdateColorButtons()
    {
        colorList.Clear();
        colorButtons.Clear();

        foreach (KeyValuePair<int, UnityEngine.Color> pair in ColorPaletteKey)
        {
            AddColorButton(pair.Key, pair.Value);
        }
    }
    #endregion
}
