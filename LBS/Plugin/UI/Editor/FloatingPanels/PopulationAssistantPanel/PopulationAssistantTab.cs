using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Core.AI.Assistant;
using ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EvaluatorElement;
using ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EvaluatorElementTitle;
using ISILab.LBS.Plugin.UI.Editor.View_Elements.Population.EVParameterElement;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using LBS.Components.TileMap;
using System.Collections.Generic;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
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
        [SerializeField] private EvaluatorsDatabase evDatabase;
        private const string evDatabase_NAME = "EvaluatorDatabase";

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
        #endregion

        #region FIELDS
        private List<PopulationMapEntry> mapEntries = new();
        private List<EvaluatorData> evaluatorsList = new();
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

        #region CONSTRUCTOR
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

            InitializeEvaluatorsDatabase();
            UpdateEvaluatorsList();
            ResetEvaluatorGen();
        }
        #endregion

        #region METHODS

        #region BLUEPRINTS_METHODS

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
        #endregion

        #region EVALUATORS_METHODS
        private void InitializeEvaluatorsDatabase()
        {
            // 1. Intentar cargar el SO desde la carpeta Resources
            // El nombre del archivo debe coincidir exactamente con evDatabase_NAME
            evDatabase = Resources.Load<EvaluatorsDatabase>(evDatabase_NAME);

            // 2. Si no existe, lo creamos
            if (evDatabase == null)
            {
                Debug.LogWarning($"No se encontró {evDatabase_NAME} en Resources. Creando una nueva instancia.");

                // Crea la instancia en la memoria RAM
                evDatabase = ScriptableObject.CreateInstance<EvaluatorsDatabase>();

                #if UNITY_EDITOR
                // Solo en el Editor lo persistimos como un archivo .asset real
                SaveAssetInEditor();
                #endif
            }

            evaluatorsList = evDatabase.Evaluators;
        }

        #if UNITY_EDITOR
        private void SaveAssetInEditor()
        {
            string folderPath = "Assets/ISILab/LBS/Plugin/Internal/Resources";

            // 1. Asegurarse de que la carpeta Resources existe
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
                // Es buena práctica refrescar el Database para que Unity vea la nueva carpeta
                UnityEditor.AssetDatabase.Refresh();
            }

            // 2. Construir la ruta completa del asset
            string fullPath = $"{folderPath}/{evDatabase_NAME}.asset";

            // 3. Crear el archivo físicamente en el disco
            UnityEditor.AssetDatabase.CreateAsset(evDatabase, fullPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"<color=green>Asset creado exitosamente en: {fullPath}</color>");
        }
        #endif

        private void UpdateEvaluatorsList()
        {
            evaluatorListView.Clear();

            //"TÍTULO PARA LA LISTA"
            EvaluatorElementTitle evElTitle = new EvaluatorElementTitle("Name", "Interfaces", "Options");
            evElTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            evaluatorListView.hierarchy.ElementAt(0).Add(evElTitle);

            foreach (EvaluatorData evData in evaluatorsList)
            {
                UpdateSingleEvaluator(evData);
            }
        }

        private void UpdateSingleEvaluator(EvaluatorData evData)
        {
            EvaluatorElement evElement = new EvaluatorElement(
                evData.Name,
                evData.Interface1,
                evData.Interface2,
                evData.Interface3,
                evData.ParamList
                );

            evElement.OnDelete += (elem) =>
            {
                // Mostramos el diálogo nativo de Unity
                bool confirm = EditorUtility.DisplayDialog(
                    "Eliminar Evaluador",               // Título
                    $"¿Estás seguro de que deseas eliminar el evaluador '{evData.Name}'?", // Mensaje
                    "Eliminar",                         // Botón de confirmar
                    "Cancelar"                          // Botón de cancelar
                );

                if (confirm)
                {
                    // Si el usuario acept�, lo borramos de la interfaz
                    // 'target' es el elemento que dispar� el evento
                    //elem.parent.hierarchy.Remove(elem); <- if i can do that why do all of this?
                    evaluatorListView.hierarchy.Remove(elem);
                    evaluatorsList.Remove(evData);
                    DeleteEvaluatorPhysicalFile(evData.Name);
                    DeleteEvaluatorVEPhysicalFile(evData.Name);
                    evDatabase.SaveDatabaseChanges();
                }
            };

            evaluatorListView.hierarchy.Add(evElement);
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
            evaluatorGeneratorInterface1.value = false;
            evaluatorGeneratorInterface2.value = false;
            evaluatorGeneratorInterface3.value = false;
        }

        public EvaluatorData ReturnEvDataWUniqueName(EvaluatorData evData)
        {
            string newName = evData.Name;
            int counter = 0;
            while (!CheckUniqueEvName(newName))
            {
                counter++;
                newName = evData.Name + "_" +counter.ToString();
            }
            evData.Name = newName;
            return evData;
        }

        public bool CheckUniqueEvName(string baseName)
        {
            bool isUniqueName = true;
            foreach (EvaluatorData evData in evaluatorsList)
            {
                if (evData.Name == baseName) isUniqueName = false;
            }

            return isUniqueName;
        }

        public void GenerateEvaluator(ClickEvent evt)
        {
            string cleanName = LBSTextUtilities.ReturnValidName(GetEvGenData().Name);
            if (!string.IsNullOrWhiteSpace(cleanName))
            {
                EvaluatorData finalEvData = GetEvGenData();
                finalEvData.Name = cleanName;
                finalEvData = ReturnEvDataWUniqueName(finalEvData);

                evaluatorsList.Add(finalEvData);
                UpdateSingleEvaluator(finalEvData);

                evDatabase.SaveDatabaseChanges();


                //llamar al creador de evaluadores y entregarle finalEvData
                EvaluatorCreator.CreateConfigurableEvaluator(finalEvData.Name, finalEvData.Interface1, finalEvData.Interface2, finalEvData.Interface3);
                ResetEvaluatorGen();
            }
            else
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Error",                                                    // Título
                    "Evaluator's name cannot be empty or have special characters other than \"_\"",                           // Mensaje
                    "OK"                                                        // Botón de cancelar
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

        private void DeleteEvaluatorPhysicalFile(string evaluatorName)
        {
            #if UNITY_EDITOR
            // 1. Obtener la ruta desde tus settings (ajusta la extensión si no es .cs)
            string folderPath = LBSSettings.Instance.paths.evaluatorsPath;
            string fileName = $"{evaluatorName}.cs";
            string fullPath = System.IO.Path.Combine(folderPath, fileName);

            // 2. Verificar si el archivo existe antes de intentar borrar
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath) != null)
            {
                bool success = AssetDatabase.DeleteAsset(fullPath);

                
                if (success)
                    Debug.Log($"<color=red>[ISILab]</color> Archivo eliminado: {fullPath}");
                else
                    Debug.LogWarning($"[ISILab] No se pudo eliminar el archivo en: {fullPath}");
                
                //Debug.Log("File to be Deleted: "+fullPath);
            }
            else
            {
                Debug.LogWarning($"[ISILab] No se encontró el archivo físico para '{evaluatorName}' en {fullPath}");
            }

            AssetDatabase.Refresh();
            #endif
        }
        private void DeleteEvaluatorVEPhysicalFile(string evaluatorName)
        {
            #if UNITY_EDITOR
            // 1. Obtener la ruta desde tus settings (ajusta la extensión si no es .cs)
            string folderPath = LBSSettings.Instance.paths.evaluatorsVEPath;
            string fileName = $"{evaluatorName+"VE"}.cs";
            string fullPath = System.IO.Path.Combine(folderPath, fileName);

            // 2. Verificar si el archivo existe antes de intentar borrar
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath) != null)
            {
                bool success = AssetDatabase.DeleteAsset(fullPath);
                
                if (success)
                    Debug.Log($"<color=red>[ISILab]</color> Archivo eliminado: {fullPath}");
                else
                    Debug.LogWarning($"[ISILab] No se pudo eliminar el archivo en: {fullPath}");
                
                //Debug.Log("File to be Deleted: "+fullPath);
            }
            else
            {
                Debug.LogWarning($"[ISILab] No se encontró el archivo físico para '{evaluatorName}' en {fullPath}");
            }

            AssetDatabase.Refresh();
            #endif
        }

        /*
        private void SaveEvaluatorDatabaseChanges()
        {
            if (evDatabase != null)
            {
                // Marca el objeto como "sucio" para que Unity sepa que debe guardarlo
                EditorUtility.SetDirty(evDatabase);

                // Fuerza el guardado de los assets modificados en el disco
                AssetDatabase.SaveAssets();

                Debug.Log("<color=orange>[ISILab]</color> Cambios en la base de datos guardados localmente.");
            }
        }
        */

        #endregion

        #endregion
    }
}