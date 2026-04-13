using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EvaluatorElement;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using LBS.Components.TileMap;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.ObjectChangeEventStream;

namespace ISILab.LBS.Editor
{
    public class PopulationAssistantTab : VisualElement
    {
        #region UXMLFACTORY
        [UxmlElementAttribute]
        public new class UxmlFactory { }
        #endregion

        private PopulationAssistantWindow window;

        #region VIEW ELEMENTS
        private VisualElement mapEliteContent;
        private VisualElement savedElitesContent;

        private Foldout mapEliteFoldout;

        private Button buttonMapElitesAssistant;

        private ListView mapElitesList;
        private AssistantMapElite target;

        //  Evaluator's Wizard 
        private LBSCustomListView       evaluatorListView;
        private LBSCustomTextField      evaluatorGeneratorName;
        private LBSCustomToggleField    evaluatorGeneratorInterface1;
        private LBSCustomToggleField    evaluatorGeneratorInterface2;
        private LBSCustomToggleField    evaluatorGeneratorInterface3;
        private LBSCustomButton         evaluatorGeneratorCreateButton;
        private LBSCustomButton         evaluatorGeneratorOpenEvFolderButton;

        // Evaluator's Parameter Editor (Should be in a new window but im writing it here in the meantime)
        private LBSCustomListView       parameterList;
        private LBSCustomTextField      parameterGeneratorName;
        private LBSCustomToggle         parameterGeneratorType;
        private LBSCustomToggleField    parameterGeneratorisList;
        private LBSCustomButton         parameterGeneratorAddButton;

        #endregion

        #region FIELDS
        private List<PopulationMapEntry> mapEntries = new();
        private List<EvaluatorData> evaluatorsList = new ();
        #endregion

        #region PROPERTIES
        protected LBSLayer TargetLayer
        {
            get => target.OwnerLayer;
        }
        List<SavedMap> SavedMapList
        {
            get => TargetLayer.Parent.GetSavedMaps(TargetLayer)?.Maps;
            set => TargetLayer.Parent.GetSavedMaps(TargetLayer).Maps = value;
        }
        List<EvaluatorData> EvaluatorsList
        {
            get => evaluatorsList;
            set => evaluatorsList = value;
        }
        #endregion

        #region STRUCTURES
        public struct EvaluatorData
        {
            public string name;
            public bool interface1;
            public bool interface2;
            public bool interface3;

            public EvaluatorData(string name, bool i1, bool i2, bool i3)
            {
                this.name = name;
                interface1 = i1;
                interface2 = i2;
                interface3 = i3;
            }
        }
        #endregion

        #region CONSTRUCTORS
        public PopulationAssistantTab(AssistantMapElite target)
        {
            this.target = target;
            window = ScriptableObject.CreateInstance<PopulationAssistantWindow>();
            window.SetAssistant(target);

            window.UpdatePins = null;
            window.UpdatePins += () =>
            {
                Debug.Log("pins updated");
                UpdateMapEntries();
                mapElitesList.Rebuild();
            };

            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PopulationAssistantTab");
            visualTree.CloneTree(this);

            //Main thing
            mapEliteFoldout = this.Q<Foldout>("FoldoutMapElites");
            mapEliteContent = this.Q<VisualElement>("MapEliteContent");

            //Assistant button
            buttonMapElitesAssistant = this.Q<Button>("ButtonMapElitesAssistant");
            buttonMapElitesAssistant.clicked += () =>
            {
                if (!window)
                {
                    window = ScriptableObject.CreateInstance<PopulationAssistantWindow>();
                    window.SetAssistant(target);
                }
                window.ShowWindow();
            };

            //savedElitesContent = this.Q<VisualElement>("SavedElitesContent");

            UpdateMapEntries();
            mapElitesList = this.Q<ListView>("MapElitesList");

            //False until I find out why it simply snaps back to the original order when you try to move something.
            //Guess that counts as disable by default anyway...? -Alice
            mapElitesList.reorderable = false;

            mapElitesList.makeItem = () => new PopulationMapEntry();

            mapElitesList.bindItem = (element, index) =>
            {
                var mapEntryVE = element as PopulationMapEntry;
                if (mapEntryVE == null) return;

                var mapEntry = SavedMapList[index];
                mapEntryVE.SetData(mapEntry);

                mapEntryVE.Name = mapEntry.Name;

                mapEntryVE.RemoveMapEntry = null;
                mapEntryVE.RemoveMapEntry += () =>
                {
                    Debug.Log("Remove at " + index);
                    mapEntries.RemoveAt(index);
                    RemoveMap(index);
                    mapElitesList.Rebuild();
                };
                mapEntryVE.ApplyMapEntry = null;
                mapEntryVE.ApplyMapEntry += () =>
                {
                    Debug.Log("applying from " + index);
                    ApplySuggestion(index);
                };
            };
            mapElitesList.itemsSource = mapEntries;

            //Evaluators
            evaluatorListView = this.Q<LBSCustomListView>("evList");
            //  Evs. Generator
            evaluatorGeneratorName = this.Q<LBSCustomTextField>("evGenName");
            evaluatorGeneratorInterface1 = this.Q<LBSCustomToggleField>("evGenToggle1");
            evaluatorGeneratorInterface2 = this.Q<LBSCustomToggleField>("evGenToggle2");
            evaluatorGeneratorInterface3 = this.Q<LBSCustomToggleField>("evGenToggle3");

            evaluatorGeneratorCreateButton = this.Q<LBSCustomButton>("evGenGenerateButton");
            evaluatorGeneratorCreateButton.RegisterCallback<ClickEvent>(GenerateEvaluator);

            evaluatorGeneratorOpenEvFolderButton = this.Q<LBSCustomButton>("evGenOpenFolderButton");
            evaluatorGeneratorOpenEvFolderButton.RegisterCallback<ClickEvent>(OpenEvaluatorsFolder);

            InitEvaluatorsList();
            ResetEvaluatorGen();
        }
        #endregion

        #region METHODS
        // should pass the preset as parameter
        /*private void AddEntry()
        {
            var mapEntry1 = ScriptableObject.CreateInstance<MAPElitesPreset>(); // not null
            savedMaps.Add(mapEntry1);

            var mapEntryVE = new PopulationMapEntry();
            mapEntries.Add(mapEntryVE);
        }*/

        private void RemoveMap(int index) => RemoveMap(SavedMapList[index].Name);
        private void RemoveMap(string name)
        {
            if (TargetLayer == null) return;
            var data = TargetLayer.Parent;
            var savedMapList = data.GetSavedMaps(TargetLayer);
            if (savedMapList == null) return;

            var maps = savedMapList.Maps;
            maps.Remove(maps.Find(c => c.Name == name));
        }
        private void UpdateMapEntries()
        {
            //Get population behavior = it's now TargetLayer!
            //Get saved maps
            if (TargetLayer == null) return;
            if (SavedMapList == null) return;

            var data = TargetLayer.Parent;

            //Clear map entries
            mapEntries.Clear();

            if (SavedMapList.Count > 0)
            {
                foreach (SavedMap map in SavedMapList)
                {
                    //Make a new visual element to set it up later
                    var mapEntryVE = new PopulationMapEntry();
                    mapEntries.Add(mapEntryVE);
                }
            }
        }
        private void ApplySuggestion(int index) => ApplySuggestion(SavedMapList[index]);
        private void ApplySuggestion(object obj)
        {
            window.OnTileMapChanged?.Invoke();
            var savedMap = obj as SavedMap;
            var chrom = savedMap.Map;
            if (chrom == null) return;

            var level = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(level, "Applying Suggestion from Saved Map");

            var layerPopulation = TargetLayer.Behaviours.Find(b => b.GetType().Equals(typeof(PopulationBehaviour))) as PopulationBehaviour;
            var rect = chrom.Rect;

            for (int i = 0; i < chrom.Length; i++)
            {
                var pos = chrom.ToMatrixPosition(i) + rect.position.ToInt();
                layerPopulation.RemoveTileGroup(pos);
                var gene = chrom.GetGene(i);
                if (gene == null)
                    continue;
                layerPopulation.AddTileGroup(pos, gene as BundleData, layerPopulation.GetActiveRotation(), null);
            }
            DrawManager.Instance.RedrawLayer(TargetLayer);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(level);
            }
            LBSMainWindow.MessageNotify(
                new LBSLog("Layer modified by Population Assistant"));

            layerPopulation.OwnerLayer.OnChangeUpdate();
        }

        //  Evaluator Wizard Methods

        private void GetAllEvaluators()
        {
            //busca todos los evaluadores del proyecto y los agrega a evaluatorsList
            //(usar Reflection, con cuidao' eso si oe)
            evaluatorsList.Add(
                new EvaluatorData
                {
                    name = "evHardcodeado",
                    interface1 = false,
                    interface2 = false,
                    interface3 = false,
                }
                );
            evaluatorsList.Add(
                new EvaluatorData
                {
                    name = "evHardcodeado2",
                    interface1 = true,
                    interface2 = false,
                    interface3 = true,
                }
                );
            evaluatorsList.Add(
                new EvaluatorData
                {
                    name = "evHardcodeado222",
                    interface1 = false,
                    interface2 = true,
                    interface3 = true,
                }
                );
            evaluatorsList.Add(
                new EvaluatorData
                {
                    name = "evHardcodeadoevHardcodeado",
                    interface1 = true,
                    interface2 = true,
                    interface3 = false,
                }
                );
            evaluatorsList.Add(
                new EvaluatorData
                {
                    name = "evHardcodeado5",
                    interface1 = true,
                    interface2 = true,
                    interface3 = true,
                }
                );
        }
        
        private void UpdateEvaluatorsList()
        {
            evaluatorListView.Clear();
            foreach (EvaluatorData evData in evaluatorsList)
            {
                UpdateSingleEvaluator(evData);
            }
        }

        private void UpdateSingleEvaluator(EvaluatorData evData)
        {
            EvaluatorElement evElement = new EvaluatorElement(
                evData.name,
                evData.interface1,
                evData.interface2,
                evData.interface3
                );

            evElement.OnDelete += (elem) =>
            {
                // Mostramos el di�logo nativo de Unity
                bool confirm = EditorUtility.DisplayDialog(
                    "Eliminar Evaluador",               // T�tulo
                    $"�Est�s seguro de que deseas eliminar el evaluador '{evData.name}'?", // Mensaje
                    "Eliminar",                         // Bot�n de confirmar
                    "Cancelar"                          // Bot�n de cancelar
                );

                if (confirm)
                {
                    // Si el usuario acept�, lo borramos de la interfaz
                    // 'target' es el elemento que dispar� el evento
                    //elem.parent.hierarchy.Remove(elem); <- if i can do that why do all of this?
                    evaluatorListView.hierarchy.Remove(elem);
                }
            };

            evaluatorListView.hierarchy.Add(evElement);
        }

        private void InitEvaluatorsList()
        {
            GetAllEvaluators();
            UpdateEvaluatorsList();
        }

        //  Evaluator generator functions
        public EvaluatorData GetEvGenData()
        {
            return new EvaluatorData(
                evaluatorGeneratorName.value,
                evaluatorGeneratorInterface1.value,
                evaluatorGeneratorInterface2.value,
                evaluatorGeneratorInterface3.value
                );
        }

        public void ResetEvaluatorGen()
        {
            evaluatorGeneratorName.value = "NewCustomEvaluator";
        }

        public EvaluatorData ReturnEvDataWUniqueName(EvaluatorData evData)
        {
            string newName = evData.name;
            int counter = 0;
            while (!CheckUniqueEvName(newName))
            {
                counter++;
                newName = evData.name + "(" +counter.ToString() + ")";
            }
            evData.name= newName;
            return evData;
        }

        public bool CheckUniqueEvName(string baseName)
        {
            bool isUniqueName = true;
            foreach (EvaluatorData evData in evaluatorsList)
            {
                if (evData.name == baseName) isUniqueName = false;
            }

            return isUniqueName;
        }

        public void GenerateEvaluator(ClickEvent evt)
        {
            string cleanName = GetEvGenData().name.Trim();
            if (!string.IsNullOrWhiteSpace(cleanName))
            {
                EvaluatorData finalEvData = GetEvGenData();
                finalEvData.name = cleanName;
                finalEvData = ReturnEvDataWUniqueName(finalEvData);

                evaluatorsList.Add(finalEvData);
                UpdateSingleEvaluator(finalEvData);

                //llamar al creador de evaluadores y entregarle finalEvData
                // double it and pass it to the seba
                EvaluatorCreator.CreateConfigurableEvaluator(finalEvData.name, finalEvData.interface1, finalEvData.interface2, finalEvData.interface3);
            }
            else
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Error",                                                   // T�tulo
                    "Evaluator's name caanot be empty",   // Mensaje
                    "OK"                                                       // Bot�n de cancelar
                );
            }
        }

        public void OpenEvaluatorsFolder(ClickEvent evt)
        {
            //string folderPath = LBSSettings.Instance.paths.evaluatorsPath + "/" + Builder.bundleName + ".asset";
            string folderPath = LBSSettings.Instance.paths.evaluatorsPath;
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);

            if (obj != null)
            {
                // Focus the Project window
                EditorUtility.FocusProjectWindow();

                // Select the object, which makes the project window jump to that folder
                Selection.activeObject = obj;

                // Optional: Ping the object to highlight it visually
                EditorGUIUtility.PingObject(obj);

                //OPEN FOLDER
                AssetDatabase.OpenAsset(obj);
            }
    }

        #endregion


    }
}