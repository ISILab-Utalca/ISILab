using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using ISILab.LBS.Plugin.Core.Settings;
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
        private LBSCustomListView evaluatorList;
        private LBSCustomTextField evaluatorGeneratorName;
        private LBSCustomToggleField evaluatorGeneratorInterface1;
        private LBSCustomToggleField evaluatorGeneratorInterface2;
        private LBSCustomToggleField evaluatorGeneratorInterface3;
        private LBSCustomButton evaluatorGeneratorCreateButton;
        private LBSCustomButton evaluatorGeneratorOpenEvFolderButton;

        // Evaluator's Parameter Editor (Should be in a new window but im writing it here in the meantime)
        private LBSCustomListView parameterList;
        private LBSCustomTextField parameterGeneratorName;
        private LBSCustomToggle parameterGeneratorType;
        private LBSCustomToggleField parameterGeneratorisList;
        private LBSCustomButton parameterGeneratorAddButton;

        #endregion

        #region FIELDS
        private List<PopulationMapEntry> mapEntries = new();
        //private List<Evaluator> evaluatorsList = new ();
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
        #endregion

        #region STRUCTURES
        public struct EvaluatorGeneratorData
        {
            public string name;
            public bool interface1;
            public bool interface2;
            public bool interface3;

            public EvaluatorGeneratorData(string name, bool i1, bool i2, bool i3)
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
            evaluatorList = this.Q<LBSCustomListView>("evList");
            evaluatorGeneratorName = this.Q<LBSCustomTextField>("evGenName");
            evaluatorGeneratorInterface1 = this.Q<LBSCustomToggleField>("evGenToggle1");
            evaluatorGeneratorInterface2 = this.Q<LBSCustomToggleField>("evGenToggle2");
            evaluatorGeneratorInterface3 = this.Q<LBSCustomToggleField>("evGenToggle3");
            evaluatorGeneratorCreateButton = this.Q<LBSCustomButton>("evGenGenerateButton");
            evaluatorGeneratorOpenEvFolderButton = this.Q<LBSCustomButton>("evGenOpenFolderButton");

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

        //  Evaluators Wizard Methods

        //GetAllEvaluators()

        //UpdateEvaluatorsList()

        public EvaluatorGeneratorData GetEvGenData()
        {
            return new EvaluatorGeneratorData(
                evaluatorGeneratorName.value,
                evaluatorGeneratorInterface1.value,
                evaluatorGeneratorInterface2.value,
                evaluatorGeneratorInterface3.value
                );
        }

        public void GenerateEvaluator()
        {
            //llamar al creador de evaluadores y entregarle GetEvGenData()
                // double it and pass it to the seba
        }

        public void OpenEvaluatorsFolder()
        {
            //  Crear LBSSettings.Instance.paths.EvaluatorFolderPath & carpeta asociada
            //string folderPath = LBSSettings.Instance.paths.bundleFolderPath + "/" + Builder.bundleName + ".asset";
            //string folderPath = LBSSettings.Instance.paths.bundleFolderPath;


            //UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);

            /*
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
            */
        }

        #endregion


    }
}