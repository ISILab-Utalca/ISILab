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
    /// <summary>
    /// The main editing window for <b>LBS Terrain Connection Grids</b>. It possesses all the main functionalities necessary to ensure their proper functioning.</br>
    /// By accessing this window, every asset inside the characteristic's associated bundle can be modified independently. 
    /// Additional tools include modifying the default generated asset under the characteristic's generatiion rule, as well as various painting tools for
    /// each asset's individual grid and a personalizable color list.
    /// </summary>
    public class TerrainConnectionGridEditorWindow : EditorWindow
    {
        #region FIELDS
        /// <summary>
        /// The target <b>LBS Terrain Connection Grid</b> to be modified with this window.
        /// </summary>
        public LBSTerrainConnectionGrid connectionGridTarget;

        //Tracking thingies
        /// <summary>
        /// The currently active terrain flag to be applied with the window's painting tools.
        /// </summary>
        public int currentColor;
        /// <summary>
        /// An enumerator dividing the three tools usable in the window.
        /// </summary>
        public enum GridTerrainTool { Brush, Fill, Eraser };
        /// <summary>
        /// Defines the currently active terrain tool out of the three available options: <b>Brush</b>, <b>Fill</b> and <b>Eraser</b>.
        /// </summary>
        private GridTerrainTool activeTool;

        //Color Buttons
        /// <summary>
        /// The container for the editor's <b>border color</b>. The editor's color palette includes a special flag (always -1) that, when applied to a particular
        /// corner of the object, will only attempt to generate its associated asset when the coordinate is detected to be a border; in other words, when the coordinate
        /// isn't connected to another block in the relevant corner.
        /// </summary>
        public VisualElement borderColorContainer;
        /// <summary>
        /// A list of every color currently associated to the <b>LBS Terrain Connection Grid</b> characteristic and their associated buttons. The buttons are
        /// additionally tied to their respective intenal terrain flags.
        /// </summary>
        public List<LBSSelectableButton> colorButtons = new List<LBSSelectableButton>();
        /// <summary>
        /// A button that, when pressed, adds a random color with a new terrain flag to the palette.
        /// </summary>
        public LBSCustomButton addColorButton;
        /// <summary>
        /// The container physically containing every color in the palette.
        /// </summary>
        public VisualElement colorList;

        //Tools
        /// <summary>
        /// A list containing all tools currently implemented into the tool.
        /// </summary>
        public List<LBSToolbarToggle> gridTerrainTools = new List<LBSToolbarToggle>();

        /// <summary>
        /// The editor's brush tool. </br>
        /// While active, clicking on any square in an asset grid will paint it with the selected color, assigning the corresponding terrain flag to it.
        /// </summary>
        public LBSToolbarToggle brushTool;
        /// <summary>
        /// The editor's fill tool. </br>
        /// While active, clicking on a square in the aasset grid will paint it. Afterwards, it'll attempt to spread into any square of the same color as the square's
        /// previous color.
        /// </summary>
        public LBSToolbarToggle fillTool;
        /// <summary>
        /// The editor's eraser tool. </br>
        /// While active, clicking on any square in an asset grid will erase its current color and neutralize its terrain flag to its default (0).
        /// </summary>
        public LBSToolbarToggle eraserTool;

        //Grids
        /// <summary>
        /// The container for every available asset grid in the <b>LBS Terrain Connection Grid</b>. Each grid will be initialized and added as an independent visual
        /// element on startup.
        /// </summary>
        public VisualElement gridsVE;

        //Zoom
        /// <summary>
        /// The slider that controls the preview scale for <b>Asset Grids</b>. It changes the scale of visible grids
        /// </summary>
        public Slider previewScaleSlider;
        /// <summary>
        /// The manipulator for the asset preview's FOV scale. It modifies how close the 3D asset's preview is to the grid representing it.
        /// </summary>
        public LBSCustomUnsignedIntegerField zoomScaleInt;
        /// <summary>
        /// Current FOV scale for asset previews.
        /// </summary>
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
        /// <summary>
        /// When called, creates a dictionary linking every color ID with their respective assigned color for visual representation. </br>
        /// Dictionaries cannot be saved as data, so this is the only current way to access both the palettes and their respective terrain flag in a single linked
        /// object.
        /// </summary>
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
        /// <summary>
        /// Points to the current active terrain tool.
        /// </summary>
        public GridTerrainTool ActiveTool => activeTool;
        #endregion

        /// <summary>
        /// Updates the FOV scale for all <b>Asset Grids</b>.
        /// </summary>
        public Action SetFOVScale;
        /// <summary>
        /// Called when the window is closed. Makes sure to clean the cache to avoid excessive generation of visual elements.
        /// </summary>
        public Action OnWindowClosed;
        /// <summary>
        /// Called when a color is removed from the palette. Updates both color lists in the target <b>Terrain Connectiion Grid</b> upon being called.
        /// </summary>
        public Action OnColorRemoved;
        /// <summary>
        /// Called when the object scale is modified in the window.
        /// </summary>
        public Action<float> OnScaleModify;

        /// <summary>
        /// Called when the confirmation window for the <b>Revert</b> button is confirmed. It resets each available <b>Asset Grid</b> to their previously saved state.
        /// </summary>
        public Action RevertChangesConfirmed;
        /// <summary>
        /// Called when the confirmation window for the <b>Clear</b> button is confirmed. It deletes all personalized terrain flags from each available <b>Asset Grid</b>,
        /// resetting each respective terrain flag to their default (0).
        /// </summary>
        public Action ClearChangesConfirmed;

        #region CONSTRUCTOR
        /// <summary>
        /// Called whenever the window is opened through the <b>Terrain Connection Grid Editor</b>. It initializes every button and necessary functionality for the window
        /// to work, as well as initializing and updating the available <b>Asset Grid</b> list for modification.
        /// </summary>
        public void CreateGUI()
        {
            connectionGridTarget.Init();
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
            revertButton.clicked += () =>
            {
                if (EditorUtility.DisplayDialog("Revert Changes", "All unsaved changes will be reverted. Continue?", "Yes", "No"))
                {
                    RevertChangesConfirmed?.Invoke();
                }
                    
            };
            //Clear button!
            clearButton = rootVisualElement.Q<Button>("ClearButton");
            clearButton.clicked += () =>
            {
                if (EditorUtility.DisplayDialog("Clear Changes", "This will clear all grids. Continue?", "Yes", "No"))
                {
                    ClearChangesConfirmed?.Invoke();
                }
            };

            //Save button!
            saveButton = rootVisualElement.Q<Button>("SaveButton");
            
            //Update button! This one didn't work so it's disabled lol
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

        /// <summary>
        /// Adds a random color key with an unused terrain flag (checked from ascending order, starting from 1) and a random color.
        /// Its respective button is then added to the color palette.
        /// </summary>
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

        /// <summary>
        /// Searches the color key corresponding to the introduced terrain flag ID and removes it. This removes it both from the visual element and the 
        /// <b>LBS Terrain Connection Grid</b> characteristic's palettes.
        /// </summary>
        /// <param name="key">The terrain flag of the color to remove.</param>
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

        /// <summary>
        /// Adds a color key to the visual element and sets it up appropriately.
        /// </summary>
        /// <param name="key">The terrain flag tied to the key.</param>
        /// <param name="color">The color to represent the terrain flag.</param>
        /// <param name="selectAfterCreation">If true, the new color key will immediately be selected upon creation. Color keys are set up to deselect
        /// any other color keys in the characteristic.</param>
        /// <param name="borderColor">If true, the new color key will be added to the border color container instead of the default color container.</param>
        /// <param name="removable">If true, the color can be manually removed from the characteristic after creation.</param>
        /// <returns>The created visual element.</returns>
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

        /// <summary>
        /// Refreshes the visual element's color key list. It completely clears the existing list, before recreating it with the characteristic's color palette.
        /// </summary>
        public void UpdateColorButtons()
        {
            colorList.Clear();
            colorButtons.Clear();

            foreach (KeyValuePair<int, UnityEngine.Color> pair in ColorPaletteKey)
            {
                AddColorButton(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Adds an <b>Asset Grid Editor</b> for each <b>Asset Grid</b> available in the characteristic, then automatically sets them up appropriately.
        /// </summary>
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
                saveButton.clicked += () => { _newGridWindow.SaveChanges(); };
                ClearChangesConfirmed += () => { _newGridWindow.ClearGrid(); };
                RevertChangesConfirmed += () => { _newGridWindow.RevertChanges(); };

                OnWindowClosed += () => { _newGridWindow.OnRemove?.Invoke(); };
                gridsVE.Add(_newGridWindow);
            }
        }
        #endregion
    }
}
