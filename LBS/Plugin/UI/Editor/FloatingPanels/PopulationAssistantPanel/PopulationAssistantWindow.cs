using ISILab.LBS.Editor.Windows;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.AI.Categorization;
using UnityEditor.UIElements;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.VisualElements.Editor.AssistantThreads;
using System.IO;
using Commons.Optimization.Evaluator;
using ISILab.AI.Optimization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Drawers;
using ISILab.Extensions;
using ISILab.AI.Categorization;
using LBS.Components.TileMap;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using LBS.Components;
using Debug = UnityEngine.Debug;

namespace ISILab.LBS.VisualElements.Editor
{

    public class PopulationAssistantWindow : EditorWindow, IAssistantThreadedEditor
    {

        #region Utilities
        private Dictionary<string, MAPElitesPreset> presetDictionary;
        private MAPElitesPreset mapEliteBundle;

        private BundleTileMap originalTileMap;
        private AssistantMapElite _assistant;

        //Default text for unchosen elements
        private const string defaultSelectText = "Select...";

        #endregion

        #region VIEW ELEMENTS
        //Preset Parameters
        private DropdownField presetField;
        private ClassDropDown param1Field;
        private ClassDropDown param2Field;
        private ClassDropDown optimizerField;

        //Parameter Information
        private Label xParamText;
        private Label yParamText;
        private Label zParamText;
        private ProgressBar xProgressBar;
        private ProgressBar yProgressBar;
        private ProgressBar zProgressBar;
        
        private VisualElement gridContent;
        
        //Visualization Information
        private SliderInt rows;
        private SliderInt columns;
        private PopulationAssistantButtonResult selectedMap;

        //Functionality buttons
        private Button recalculate;
        private Button applySuggestion;
        private Button resetButton;
        private Button closeWindow;
        
        //Scriptable Object Manipulation
        private ObjectField presetFieldRef;
        private Button openPresetButton;
        private Button resetPresetButton;
        private Button autoSelectButton;
        
        //Parameters' graphic
        private VisualElement graphOfHell;

        //Layer Context
        private ListView layerList;
        private Button addLayerButton;
        private VisualElement lockedContextEntryContainer;

        #endregion

        #region PROPERTIES

        private IRangedEvaluator currentXField
        {
            get => mapEliteBundle?.XEvaluator;
            set
            {
                if (mapEliteBundle == null) return;
                mapEliteBundle.XEvaluator = value;
            }
        }

        private IRangedEvaluator currentYField
        {
            get => mapEliteBundle?.YEvaluator;
            set
            {
                if (mapEliteBundle == null) return;
                mapEliteBundle.YEvaluator = value;
            }
        }

        private BaseOptimizer currentOptimizer => mapEliteBundle?.Optimizer;

        private PopulationAssistantGraph CurrentGraph { get; set; }

        private PopulationBehaviour LayerPopulation => _assistant.LayerPopulation;

        private LBSLevelData Data => LayerPopulation.OwnerLayer.Parent;

        #endregion

        #region EVENTS
        public Action OnTileMapChanged;
        private Action OnTileMapReset;
        public Action UpdatePins;
        private Action<IOptimizable> OnValuesUpdated;
        private Stopwatch sw;

        #endregion
        
        #region METHODS
        
        #region GUI
        public void CreateGUI()
        {
            if(LayerPopulation is null) Close();
            
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PopulationAssistantWindow");
            visualTree.CloneTree(rootVisualElement);
            
            SetUpPreset();
            
            SetUpOptimizer();

            // preset settings
            SetUpPresets();

            // grid
            SetUpGrid();

            // buttons lower bar
            SetUpButtons();

            // graph of hell
            SetUpGraph();

            //LAYER CONTEXT
            SetUpLayerContext();
        }

        private void SetUpOptimizer()
        {
            optimizerField = rootVisualElement.Q<ClassDropDown>("ZParamDropdown");
            optimizerField.Type = typeof(IRangedEvaluator);
            optimizerField.value = defaultSelectText;
            optimizerField.RegisterValueChangedCallback(_ =>
            {
                if (optimizerField.value == null) return;
                if (optimizerField.value == currentOptimizer?.Evaluator?.GetType().Name) return;
                if (currentOptimizer == null) return;

                var optimizerChoice = optimizerField.GetChoiceInstance() as IRangedEvaluator;
                currentOptimizer.Evaluator = optimizerChoice;
                InitializeEvaluator(optimizerChoice);
                zParamText.text = new string("Fitness ("+optimizerField.Value+")");

            });
            optimizerField.SetEnabled(false);
        }

        private void SetUpPreset()
        {
            presetDictionary = new Dictionary<string, MAPElitesPreset>();
            presetField = rootVisualElement.Q<DropdownField>("Preset");
            SetPresets();
            presetField.value = "Select Preset";
            presetField.RegisterValueChangedCallback(evt =>
            {
                UpdatePreset(evt.newValue);
                LBSMainWindow.MessageNotify(
                    new LBSLog($"Selected MAP Elite preset: {evt.newValue}"));
            });

            //Progress Bar and Sliders
            xParamText = rootVisualElement.Q<Label>("XParamText");
            yParamText = rootVisualElement.Q<Label>("YParamText");
            zParamText = rootVisualElement.Q<Label>("ZParamText");
            xProgressBar = rootVisualElement.Q<ProgressBar>("XProgressBar");
            yProgressBar = rootVisualElement.Q<ProgressBar>("YProgressBar");
            zProgressBar = rootVisualElement.Q<ProgressBar>("ZProgressBar");


            if (xProgressBar != null) { xProgressBar.value = 0; xProgressBar.title = "0%"; }
            if (yProgressBar != null) { yProgressBar.value = 0; yProgressBar.title = "0%"; }
            if (zProgressBar != null) { zProgressBar.value = 0; zProgressBar.title = "0%"; }


            //Set parameters. Make everyone a ranged evaluator, make the value a default, add the listener to change the chosen elite bundle and then disable it.
            //I set everything false so they can't be manipulated if there's no preset present.
            param1Field = rootVisualElement.Q<ClassDropDown>("XParamDropdown");
            param1Field.Type = typeof(IRangedEvaluator);
            param1Field.value = defaultSelectText;
            param1Field.RegisterValueChangedCallback(_ =>
            {
                //Failsafe stuff
                if (param1Field.value == null) return;
                if (param1Field.value == currentXField?.GetType().Name) return;

                //Choice change
                var xChoice = param1Field.GetChoiceInstance() as IRangedEvaluator;
                currentXField = xChoice;
                InitializeEvaluator(xChoice);
                xParamText.text = param1Field.Value;

            });
            param1Field.SetEnabled(false);

            //Param 2
            param2Field = rootVisualElement.Q<ClassDropDown>("YParamDropdown");
            param2Field.Type = typeof(IRangedEvaluator);
            param2Field.value = defaultSelectText;
            param2Field.RegisterValueChangedCallback(_ =>
            {
                //Failsafe stuff
                if (param2Field.value == null) return;
                if (param2Field.value == currentYField?.GetType().Name) return;

                //Choice change
                var yChoice = param2Field.GetChoiceInstance() as IRangedEvaluator;
                currentYField = yChoice;
                InitializeEvaluator(yChoice);
                yParamText.text = param2Field.Value;
            });
            param2Field.SetEnabled(false);



        }

        private void SetUpPresets()
        {
            //Preset field (always disabled)
            presetFieldRef = rootVisualElement.Q<ObjectField>("PresetObjectField");
            presetFieldRef.SetEnabled(false);

            //Preset manipulation buttons
            openPresetButton = rootVisualElement.Q<Button>("OpenPresetButton");
            openPresetButton.clicked += () => 
            {
                if (presetField.value is not null)
                    Selection.activeObject = presetFieldRef.value;
                else LBSMainWindow.MessageNotify(
                    new LBSLog("No preset selected.", LogType.Warning));
            };

            resetPresetButton = rootVisualElement.Q<Button>("ResetPresetButton");
            resetPresetButton.clicked += () =>
            {
                if (mapEliteBundle != null) mapEliteBundle = mapEliteBundle.ResetValues();
                UpdatePreset(mapEliteBundle.PresetName);
            };

            autoSelectButton = rootVisualElement.Q<Button>("AutoSelectButton");
            autoSelectButton.clicked += () =>
            {
                _assistant.AutoSelectArea(out List<string> logs);
                logs.ForEach(log => LBSMainWindow.MessageNotify(
                    new LBSLog(log, LogType.Warning, 5)));
                DrawManager.Instance.RedrawLayer(_assistant.OwnerLayer);
            };
            
        }

        private void SetUpGrid()
        {
            rows = rootVisualElement.Q<SliderInt>("RowsSlideInt");
            rows.RegisterValueChangedCallback(_ => UpdateGrid());
            columns = rootVisualElement.Q<SliderInt>("ColumnsSlideInt");
            columns.RegisterValueChangedCallback(_ => UpdateGrid());

            gridContent = rootVisualElement.Q<VisualElement>("GridContent");
            UpdateGrid();
        }

        private void SetUpGraph()
        {
            graphOfHell = rootVisualElement.Q<VisualElement>("GraphOfHell");
            
            //Create and add VisualElement: PopulationAssistantGraph to the container
            CurrentGraph = new(new[] { 0f, 0f, 0f }, 2);
            SetGraph();
            graphOfHell.Add(CurrentGraph);
        }

        private void SetUpButtons()
        {
            //Recalculate button
            recalculate = rootVisualElement.Q<Button>("ButtonRecalculate");
            recalculate.clicked += RunAlgorithm;

            //Suggestion button
            applySuggestion = rootVisualElement.Q<Button>("ButtonApplySuggestion");
            applySuggestion.clicked += ApplySuggestion;

            //Reset button
            originalTileMap = LayerPopulation.BundleTilemap.Clone() as BundleTileMap;
            resetButton = rootVisualElement.Q<Button>("ButtonReset");
            resetButton.clicked += ResetSuggestion;
            resetButton.SetEnabled(false);
            
            OnTileMapChanged += () => {
                originalTileMap = LayerPopulation.BundleTilemap.Clone() as BundleTileMap;
                resetButton.SetEnabled(originalTileMap!=null);
            };
            OnTileMapReset += () => 
            {
                originalTileMap = null;
                resetButton.SetEnabled(false);
            };
            
            //Close button...?
            closeWindow = rootVisualElement.Q<Button>("ButtonClose");
            closeWindow.clicked += Close;
        }

        private void SetUpLayerContext()
        {
            lockedContextEntryContainer = rootVisualElement.Q<VisualElement>("LockedLayerContainer");
            AddLockedLayer();

            layerList = rootVisualElement.Q<ListView>("LayerList");
            //Data.OnChanged += (_) => OnCheckLayerRemoved?.Invoke(_.GetLayer());
            layerList.reorderable = false;
            layerList.makeItem += () => new LayerContextEntry();
            layerList.bindItem = (element, index) =>
            {
                var layerContextVE = element as LayerContextEntry;
                if (layerContextVE == null) return;

                layerContextVE.UpdateData(Data.ContextLayers[index]);
                LBSLayer layer = layerContextVE.LayerReference;
                Data.OnContextChanged += (_) => 
                {
                    layerList.Rebuild();
                };
                layerContextVE.EvaluateOverlap(Data.ContextLayers);
                layerContextVE.OnRemoveButtonClicked = null;
                layerContextVE.OnRemoveButtonClicked += () =>
                {
                    ToggleLayerContext(layer);
                    //Data.ContextLayers.RemoveAt(index);
                    //layerList.Remove(element);
                    //layerList.Rebuild();
                };
            };

            layerList.itemsSource = Data.ContextLayers;

            addLayerButton = rootVisualElement.Q<Button>("AddLayerButton");
            addLayerButton.clicked += AddLayerMenu;
        }

        #endregion
        
        private void OnDestroy()
        {
            _assistant.RequestOptimizerStop();
        }
        
        //Set assistant for window
        public void SetAssistant(AssistantMapElite target)
        {
            _assistant = target;
        }

        //Create the window
        public void ShowWindow()
        {
            titleContent = new GUIContent("Population Assistant");
            minSize = new Vector2(1000, 500); // use the Canvas Size of the uxml
            Show();
        }
        
        private void UpdateTooltips()
        {
            param1Field.tooltip = currentXField?.Tooltip;
            param2Field.tooltip = currentYField?.Tooltip;
            optimizerField.tooltip = currentOptimizer?.Evaluator?.Tooltip;
        }
        
        #region Presets
        //Add all presets in the preset directory to the preset dropdown
        private void SetPresets()
        {
            var settings = LBSSettings.Instance;
            var presetPath = settings.paths.assistantPresetFolderPath;
            
            //Directory making
            var info = new DirectoryInfo(presetPath);
            //Debug.Log(presetPath);
            var fileInfo = info.GetFiles();

            //Find all presets in the directory
            var mapPresets = new List<MAPElitesPreset>();
            foreach (var file in fileInfo)
            {
                var newPreset = AssetDatabase.LoadAssetAtPath<MAPElitesPreset>(presetPath + "\\" + file.Name);
                if (newPreset != null)
                {
                    //Debug.Log("loaded: " + newPreset);
                    mapPresets.Add(newPreset);
                }
            }

            //Just in case
            presetField.choices.Clear();

            //Add presets found in the dictionary
            foreach(var preset in mapPresets)
            {
                presetField.choices.Add(preset.PresetName);
                presetDictionary.Add(preset.PresetName, preset);
            }
        }
        
        //Update preset whenever it's changed with its respective stats
        private void UpdatePreset(string value)
        {
            //Disable parameters unless the preset is valid. Otherwise, enable them since they can be manipulated.
            if (value == null || !presetDictionary.TryGetValue(value, out var preset))
            {
                param1Field.SetEnabled(false);
                param2Field.SetEnabled(false);
                optimizerField.SetEnabled(false);
                return;
            }
            
            //Set the map elite accordingly.
            mapEliteBundle = preset;
            _assistant.LoadPresset(mapEliteBundle);
            presetFieldRef.value = mapEliteBundle;
            rows.value = mapEliteBundle.SampleCount.x;
            columns.value = mapEliteBundle.SampleCount.y;

            //Enable params set the preset things to the new choice.
            param1Field.SetEnabled(true);
            param2Field.SetEnabled(true);
            optimizerField.SetEnabled(true);

            param1Field.Value = currentXField != null ? currentXField.GetType().Name : defaultSelectText;
            param2Field.Value = currentYField != null ? currentYField.GetType().Name : defaultSelectText;
            optimizerField.value = currentOptimizer?.Evaluator != null ? currentOptimizer.Evaluator.GetType().Name : defaultSelectText;

            InitializeAllCurrentEvaluators();
            originalMapCalcs();

            //param1Field.tooltip = currentXField.Tooltip;
            //param2Field.tooltip = currentYField.Tooltip;
            //optimizerField.tooltip = currentOptimizer?.Evaluator.Tooltip;

            //InitializeAllCurrentEvaluators();
        }

        //Calcs from the original map to set initial values & update graph
        private void originalMapCalcs()
        {
            Vector3 scores = _assistant.EvaluateOriginalMap();

            string textX = (scores.x > -1000) ? $" [Actual: {scores.x:0.00}]" : "";
            string textY = (scores.y > -1000) ? $" [Actual: {scores.y:0.00}]" : "";
            string textZ = (scores.z > -1000) ? $" [Actual: {scores.z:0.00}]" : "";

            yParamText.text = param2Field.Value;
            xParamText.text = param1Field.Value;
            zParamText.text = new string("Fitness (" + optimizerField.Value + ")");

            if (scores.x > -1000)
            {
                xProgressBar.value = scores.x;
                yProgressBar.value = scores.y;
                zProgressBar.value = scores.z;
                xProgressBar.title = Mathf.FloorToInt(scores.x * 100).ToString() + "%";
                yProgressBar.title = Mathf.FloorToInt(scores.y * 100).ToString() + "%";
                zProgressBar.title = Mathf.FloorToInt(scores.z * 100).ToString() + "%";
                CurrentGraph.SetAxisValue(scores.z, 0);
                CurrentGraph.SetAxisValue(scores.x, 1);
                CurrentGraph.SetAxisValue(scores.y, 2);

                CurrentGraph.RecalculateCorners();
                CurrentGraph.MarkDirtyRepaint();
            }
        }

        private void DefaultGraphsValues()
        {
            if (CurrentGraph != null)
            {
                CurrentGraph.SetAxisValue(0f, 0);
                CurrentGraph.SetAxisValue(0f, 1);
                CurrentGraph.SetAxisValue(0f, 2);

                CurrentGraph.RecalculateCorners();
                CurrentGraph.MarkDirtyRepaint();
            }

            if (xProgressBar != null) { xProgressBar.value = 0; xProgressBar.title = "0%"; }
            if (yProgressBar != null) { yProgressBar.value = 0; yProgressBar.title = "0%"; }
            if (zProgressBar != null) { zProgressBar.value = 0; zProgressBar.title = "0%"; }

            yParamText.text = param2Field.Value;
            xParamText.text = param1Field.Value;
            zParamText.text = new string("Fitness (" + optimizerField.Value + ")");
        }

        #endregion
       
        #region Evaluators
        
        private void InitializeAllCurrentEvaluators()
        {
            var evalList = new List<IEvaluator>();
            if (currentXField != null) { evalList.Add(currentXField); }
            if (currentYField != null) { evalList.Add(currentYField); }
            if (currentOptimizer?.Evaluator != null) { evalList.Add(currentOptimizer.Evaluator); }
            if (evalList.Count == 0) return;

            InitializeEvaluator(evalList.ToArray());
        }

        private void InitializeEvaluator(params IEvaluator[] evaluators)
        {
            foreach (var evaluator in evaluators)
            {
                InitializeEvaluator(evaluator);
            }
        }

        private void InitializeEvaluator(IEvaluator evaluator)
        {
            _assistant.InitializeEvaluator(evaluator);

            UpdateTooltips();
        }

        #endregion
        
        //Run the algorithm for suggestions
        private void RunAlgorithm()
        {
            if(mapEliteBundle == null)
            {
                LBSMainWindow.MessageNotify(
                    new LBSLog("MAP Elite Preset not selected or null.", LogType.Error, 5));
                Debug.LogError("[ISI Lab]: MAP Elite Preset not selected or null.");
                return;
            }

            //Check if there's a place to optimize
            if (_assistant.RawToolRect.width == 0 || _assistant.RawToolRect.height == 0)
            {
                LBSMainWindow.MessageNotify(
                    new LBSLog("Use the Area Selector tool to select an area to optimize before starting MAP Elites.", LogType.Error, 5));
                Debug.LogError("[ISI Lab]: Selected evolution area height or width < 0");
                return;
            }

            UpdateGrid();

            InitializeAllCurrentEvaluators();

            //This resets the algorithm all the time, so nothing to worry about regarding whether it's running or not. /// Not sure about that...
            _assistant.LoadPresset(mapEliteBundle);

            sw = new Stopwatch();
            
            _assistant.SetAdam(_assistant.RawToolRect, Data.ContextLayers);
            recalculate.text = "Recalculate";
            
            LBSMainWindow.MessageNotify(new LBSLog("Calculating."));
            sw.Start();
            RunExecuteTask();
            DefaultGraphsValues();
        }
        

        #region IAssistantThreadedEditor
        public CancellationToken CancelToken { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public Plugin.UI.Editor.Windows.ToolBar.ToolBarMain TaskBar { get; set; }
        void IAssistantThreadedEditor.OnAssistantTermination(string log, LogType type)
        {
            EditorApplication.delayCall += () =>
            {
                sw.Stop();
                LBSMainWindow.MessageNotify(
                    new LBSLog($"MAP Elites finished. ({sw.ElapsedMilliseconds} ms.)",
                    LogType.Log, 5));
                Debug.Log($"MAP Elites finished. ({sw.ElapsedMilliseconds} ms.)");
                
                TaskBar.EnableProcess(false);
                UpdateContent();
                Repaint();
            };
        }
        #endregion
        
        private void RunExecuteTask()
        {
            ((IAssistantThreadedEditor)this).SetUpTask(this, _assistant);
            Task.Run(() =>
            {
                try
                {
                    _assistant.Execute(false, ((IAssistantThreadedEditor)this).ReportProgress, CancelToken);
                }
                catch (Exception ex)
                {
                    ((IAssistantThreadedEditor)this).OnTaskException(ex, _assistant);
                }
            }, CancelToken);
        }
        
        #region Suggestion
        //Apply the suggestion in the world
        private void ApplySuggestion() => ApplySuggestion(selectedMap.Data);
        private void ApplySuggestion(object obj)
        {
            //This MUST go first since it'll save the original tilemap
            OnTileMapChanged?.Invoke();

            BundleTilemapChromosome chromosome = obj as BundleTilemapChromosome;
            if (chromosome == null)
            {
                if (selectedMap.Data == null)
                {
                    if (selectedMap.Data != null)
                    {
                        throw new Exception("[ISI Lab] Data " + selectedMap.Data.GetType().Name +
                                            " is not LBSChromosome!");
                    }
                }
            }

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Add Element population");

            if (chromosome != null)
            {
                var rect = chromosome.Rect;

                for (int i = 0; i < chromosome.Length; i++)
                {
                    var pos = chromosome.ToMatrixPosition(i) + rect.position.ToInt();
                    LayerPopulation.RemoveTileGroup(pos);
                    var gene = chromosome.GetGene(i);
                    if (gene == null)
                        continue;
                    LayerPopulation.AddTileGroup(pos, gene as BundleData, LayerPopulation.GetActiveRotation());
                }
            }

            DrawManager.Instance.RedrawLayer(_assistant.OwnerLayer);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
            LBSMainWindow.MessageNotify(new LBSLog("Layer modified by Population Assistant"));

            LayerPopulation.OwnerLayer.OnChangeUpdate();
        }
        
        private void PinSuggestion(object obj)
        {
            //Get chromosome
            if (obj is PopulationAssistantButtonResult objVE)
            {
                var suggestionData = objVE.Data as BundleTilemapChromosome;
                if (suggestionData == null)
                {
                    throw new Exception("[ISI Lab] Data " + selectedMap.Data.GetType().Name + " is not LBSChromosome!");
                }

                //Create bundle tile map
                var newTileMap = new BundleTileMap();
                for (int i = 0; i < suggestionData.GetGenes().Length; i++)
                {
                    if (suggestionData.GetGenes()[i] == null) continue;
                    if (suggestionData.GetGenes()[i] is BundleData geneData)
                    {
                        newTileMap.AddGroup(new TileBundleGroup(suggestionData.ToMatrixPosition(i),
                            geneData.Bundle.TileSize, geneData, Vector2.right));
                    }
                }

                //Get level data and layer
                var layer = LayerPopulation.OwnerLayer;
                var levelData = LayerPopulation.OwnerLayer.Parent;
                var savedMapList = levelData.GetSavedMaps(layer);
                if(savedMapList!=null)
                {
                    //Check for duplicates
                    foreach (SavedMap storedMap in savedMapList.Maps)
                    {
                        if (!suggestionData.Equals(storedMap.Map)) continue;
                        
                        LBSMainWindow.MessageNotify(
                            new LBSLog("An equal suggestion already exists.", LogType.Warning));
                        return;
                    }
                }
                var newSavedMap = new SavedMap(suggestionData, "", (float)suggestionData.Fitness)
                {
                    Image = objVE.GetTexture()
                };
                levelData.SaveMapInLayer(newSavedMap, layer);
            }

            LBSMainWindow.MessageNotify(new LBSLog("Suggestion pinned."));
            UpdatePins?.Invoke();

        }

        //Reset the suggestion to its original form
        private void ResetSuggestion()
        {
            if (originalTileMap == null) return;
            if (LayerPopulation.Tilemap == null)
            {
                LBSMainWindow.MessageNotify(
                    new LBSLog("Layer tile map not found.")); return;
            }

            

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Add Element population");

            LayerPopulation.ReplaceTileMap(originalTileMap);
            DrawManager.Instance.RedrawLayer(_assistant.OwnerLayer);
            LBSMainWindow.MessageNotify(
                new LBSLog("Layer reset."));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
            LayerPopulation.OwnerLayer.OnChangeUpdate();
            OnTileMapReset?.Invoke();

            originalMapCalcs();
        }
        #endregion

        #region GRID-RELATED METHODS
        
        //Update all squares in the grid
        private void UpdateContent()
        {
            var veChildren = GetButtonResults(new List<PopulationAssistantButtonResult>(), gridContent);
            for (int i = 0; i < _assistant.toUpdate.Count &&
                            i < veChildren.Count; i++)
            {
                var v = _assistant.toUpdate[i];
                var index = (int)(v.y * _assistant.SampleWidth + v.x);

                SetBackgroundTexture(veChildren[index], _assistant.RawToolRect);

                veChildren[index].Data = _assistant.Samples[(int)v.y, (int)v.x];
                veChildren[index].Score = ((decimal)_assistant.Samples[(int)v.y, (int)v.x].Fitness).ToString("f4");
                var t = veChildren[index].GetTexture();
                veChildren[index].SetTexture(veChildren[index].Data != null
                    ? veChildren[index].backgroundTexture.MergeTextures(t).FitSquare()
                    : DirectoryTools.GetAssetByName<Texture2D>("LoadingContent"));

                veChildren[index].UpdateLabel();
            }
            _assistant.toUpdate.Clear();
        }
        
        //Redraws the grid
        private void UpdateGrid()
        {
            if(mapEliteBundle!=null)
            {
                mapEliteBundle.SampleCount = new Vector2Int(rows.value, columns.value);
            }

            gridContent.Clear();
            gridContent.style.flexDirection = FlexDirection.ColumnReverse;
            List<VisualElement> rowsVE = new();
            for (int i = 0; i < rows.value; i++)
            {
                var newRowVE =  new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        flexGrow = 1
                    }
                };
                rowsVE.Add(newRowVE);
            }
            foreach (var rVE in rowsVE)
            {
                for (int i = 0; i < columns.value; i++)
                {
                    var resultVE = new PopulationAssistantButtonResult
                    {
                        style =
                        {
                            flexGrow = 1,
                            alignSelf = Align.Stretch
                        }
                    };
                    rVE.Add(resultVE);
                    resultVE.selectButton.clicked += () => ShowButtonStats(resultVE);
                    resultVE.OnApplySuggestion += () => ApplySuggestion(resultVE.Data);
                    resultVE.OnSaveSuggestion += () => PinSuggestion(resultVE);
                }
            }
          
            foreach (var rVE in rowsVE)
            {
                gridContent.Add(rVE);
            }
        }
        
        
        //Return a list of all buttons available.
        private List<PopulationAssistantButtonResult> GetButtonResults(List<PopulationAssistantButtonResult> buttons, VisualElement parent)
        {
            foreach (var ve in parent.Children())
            {
                if (ve is PopulationAssistantButtonResult buttonResult)
                {
                    buttons.Add(buttonResult);
                }

                // Recurse on children
                GetButtonResults(buttons, ve);
            }
            return buttons;
        }
        
        //Change the texture of a specific button
        private void SetBackgroundTexture(PopulationAssistantButtonResult gridSquare, Rect rect)
        {
            var behaviours = _assistant.OwnerLayer.Parent.ContextLayers.SelectMany(l => l.Behaviours);
            var bh = _assistant.OwnerLayer.GetBehaviour<PopulationBehaviour>();

            var size = 16;
            var textures = new List<Texture2D>();

            foreach (var b in behaviours)
            {
                if (b == null) continue;
                if (bh != null && b.Equals(bh)) continue;
                
                Type drawerT = LBS_Editor.GetDrawer(b.GetType());
                if (Activator.CreateInstance(drawerT) is Drawer drawer)
                {
                    textures.Add(drawer.GetTexture(b, rect, Vector2Int.one * size));
                }
            }

            
            var texture = new Texture2D((int)(rect.width * size), (int)(rect.height * size));

            for (int j = 0; j < texture.height; j++)
            {
                for (int i = 0; i < texture.width; i++)
                {
                    texture.SetPixel(i, j, new Color(0.25f, 0.25f, 0.25f, 1));
                }
            }

            for (int i = textures.Count - 1; i >= 0; i--)
            {
                if (textures[i] == null) continue;
                texture = texture.MergeTextures(textures[i]);
            }

            texture.Apply();

            //Update texture on the chosen square
            gridSquare.SetTexture(texture);
        }

        //Show the stats for any button selected
        private void ShowButtonStats(PopulationAssistantButtonResult buttonResult)
        {
            var mapData = buttonResult.Data as IOptimizable;

            //Shows data if non null
            if (mapData == null) return;

            xProgressBar.value = (float)mapData.xFitness;
            yProgressBar.value = (float)mapData.yFitness;
            zProgressBar.value = (float)mapData.Fitness;
            xProgressBar.title = Mathf.FloorToInt((float)mapData.xFitness * 100).ToString() + "%";
            yProgressBar.title = Mathf.FloorToInt((float)mapData.yFitness * 100).ToString() + "%";
            zProgressBar.title = Mathf.FloorToInt((float)mapData.Fitness * 100).ToString() + "%";

            //Takes border off selected map previously selected map
            selectedMap?.OnButtonDeselected?.Invoke();
            selectedMap = buttonResult;
            buttonResult.OnButtonSelected?.Invoke();
            OnValuesUpdated?.Invoke(mapData);
        }

        private void SetGraph()
        {
            //Modify graph's colors (not necessary, it comes with default colors)
            CurrentGraph.MainColor = Color.green;
            CurrentGraph.SecondaryColor = Color.cyan;

            CurrentGraph.SetAxisColor(Color.blue, 0);
            CurrentGraph.SetAxisColor(Color.red, 1);
            CurrentGraph.SetAxisColor(Color.green, 2);

            OnValuesUpdated = null;
            OnValuesUpdated += (optimizable) => {
                IOptimizable opt = optimizable;
                CurrentGraph.SetAxisValue((float)opt.Fitness, 0);
                CurrentGraph.SetAxisValue((float)opt.xFitness, 1);
                CurrentGraph.SetAxisValue((float)opt.yFitness, 2);
                CurrentGraph.RecalculateCorners();
                CurrentGraph.MarkDirtyRepaint();
            };
        }


        #endregion

        #region LAYER CONTEXT METHODS

        private void AddLockedLayer()
        {
            //Add the layer to Layer context
            var lockedLayer = new LayerContextEntry();
            lockedLayer.UpdateData(_assistant.OwnerLayer);
            lockedLayer.SetEnabled(false);
            lockedContextEntryContainer.Add(lockedLayer);
        }

        private void AddLayerMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach(LBSLayer layer in Data.Layers)
            {
                //The layer the assistant is working on can't be used as context, since its content is overwritten.
                if (!_assistant.OwnerLayer.Equals(layer))
                { 
                    menu.AddItem(new GUIContent(layer.Name), Data.ContextLayers.Contains(layer), ToggleLayerContext, layer); 
                }
            }
            menu.ShowAsContext();
        }

        private void ToggleLayerContext(object layer)
        {
            LBSLayer objectLayer = layer as LBSLayer;
            if (objectLayer == null)
            {
                Debug.LogError("Object Layer was null.");
                return;
            }                

            if(Data.ContextLayers.Contains(layer))
            {
                Data.RemoveLayerFromContext(objectLayer);
            }
            else
            {
                Data.ContextLayers.Add(objectLayer);
                objectLayer.OnContextAddInvoke();
            }
            layerList.Rebuild();

            InitializeAllCurrentEvaluators();
        }

        #endregion
        
        #endregion
    }
}