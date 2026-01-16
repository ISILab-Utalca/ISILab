using System;
using System.Collections.Generic;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.VisualElements.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Plugin.UI.Editor.Windows.BundleCharacteristics;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

namespace ISILab.LBS.Plugin.UI.Editor.Windows.BundleCharacteristics
{
    public class TerrainConnectionGridEditorWindow : EditorWindow
    {
        #region FIELDS
        public LBSTerrainConnectionGrid connectionGridTarget;

        //Tracking thingies
        public int currentColor;
        public enum GridTerrainTool { Brush, Fill, Eraser };
        private GridTerrainTool activeTool;

        //Color Buttons
        public VisualElement borderColorContainer;

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
        private Button updateButton;

        private List<AssetGridEditorWindow> editorWindows;

        //Default Asset
        private IntegerField defaultAssetField;
        private Button defaultMinus;
        private Button defaultPlus;
        private Toggle defaultHighlight;

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
            connectionGridTarget.UpdateGridList();

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

            borderColorContainer = rootVisualElement.Q<VisualElement>("BorderColor");
            //Button for specifically the creation of borders. It isn't saved on the color palette or anything because the palette can never access -1 on its own.
            var borderColorButton = AddColorButton(-1, new Color(0.8f, 0.8f, 0.8f), false, true, false);
            borderColorButton.tooltip = "Tiles painted with this color will be ignored unless specifically working as compatible borders.";

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
            //Update button!
            updateButton = rootVisualElement.Q<Button>("UpdateButton");
            updateButton.clicked += () => {
                connectionGridTarget.UpdateGridList();
                PaintGridListPanel();
            };

            //Icons!
            editorWindows = new List<AssetGridEditorWindow>();
            gridsVE = rootVisualElement.Q<VisualElement>("GridsVE");
            PaintGridListPanel();

            saveButton.clicked += () => {
                EditorUtility.SetDirty(connectionGridTarget.Owner);
                AssetDatabase.SaveAssets();
            };

            //Default editor button!
            defaultAssetField = rootVisualElement.Q<IntegerField>("DefaultAssetField");
            defaultAssetField.value = connectionGridTarget.DefaultAsset;
            defaultAssetField.maxLength = connectionGridTarget.GridList.Count;

            defaultAssetField.RegisterValueChangedCallback((evt) => {
                connectionGridTarget.DefaultAsset = evt.newValue;
                defaultAssetField.SetValueWithoutNotify(connectionGridTarget.DefaultAsset);

                if (defaultHighlight.value == true)
                {
                    if((evt.previousValue <= connectionGridTarget.GridList.Count) && (defaultAssetField.value <= connectionGridTarget.GridList.Count))
                    {
                        var oldbutton = gridsVE[evt.previousValue] as AssetGridEditorWindow;
                        var button = gridsVE[defaultAssetField.value] as AssetGridEditorWindow;
                        if (oldbutton != null) oldbutton.ToggleHighlight(false);
                        if (button != null) button.ToggleHighlight(true);
                    }
                }
            });

            defaultMinus = rootVisualElement.Q<Button>("DefaultMinus");
            defaultMinus.clicked += () => { if (defaultAssetField.value > 0) defaultAssetField.value--; };

            defaultPlus = rootVisualElement.Q<Button>("DefaultPlus");
            defaultPlus.clicked += () => { if (defaultAssetField.value < defaultAssetField.maxLength) defaultAssetField.value++; };

            defaultHighlight = rootVisualElement.Q<Toggle>("DefaultHighlight");
            defaultHighlight.RegisterValueChangedCallback((evt) => {
                var button = gridsVE[defaultAssetField.value] as AssetGridEditorWindow;
                if (button != null) button.ToggleHighlight(evt.newValue);

            });


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
            int _counter = 0;
            for(int i=1; i<ColorPaletteKey.Count+2; i++)
            {
                if(!ColorPaletteKey.ContainsKey(i))
                {
                    _counter = i;
                    break;
                }
            }
            Color _color = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.3f, 1f);
            connectionGridTarget.AddColor(_counter, _color);
            AddColorButton(_counter, _color, true);
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

        public LBSSelectableButton AddColorButton(int key, Color color, bool selectAfterCreation = false, bool borderColor = false, bool removable = true)
        {
            //Add button and store its key as data
            var newButton = new LBSSelectableButton(color, removable);
            newButton.Data = key;

            newButton.tooltip = "Color ID: " + key;

            //Add button functionality
            newButton.OnExecute += () => {
                foreach(LBSSelectableButton button in colorButtons) { 
                    button.ToggleButtonSelected(false); }
                newButton.ToggleButtonSelected(true);
                currentColor = newButton.Data;
            };
            //this is meant to pick the color from colorButtons btw!
            newButton.OnRemove += () => { colorButtons.Remove(newButton); RemoveColorKey(newButton.Data); };

            //Add to the visual window...
            if(borderColor)
            {
                //Addendum: I added this later so I didn't have to recycle that much code to add a border color to this. tl;dr this is just executed once when the window is initialized.
                borderColorContainer.Add(newButton);
                
            } else
            {
                colorList.Add(newButton);
            }
            //...And to the button list
            colorButtons.Add(newButton);

            //Also let's select it, because why not!
            if (selectAfterCreation) newButton.OnExecute?.Invoke();

            //Just in case!
            return newButton;
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

        public void PaintGridListPanel()
        {
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
        }
        #endregion
    }
}
