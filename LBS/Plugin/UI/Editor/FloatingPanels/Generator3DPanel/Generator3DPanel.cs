using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;

using ISILab.LBS.Plugin.MapTools.Generators;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor.Windows;

using LBS.Components;

namespace ISILab.LBS.VisualElements.Editor
{


    [UxmlElement]
    public partial class Generator3DPanel : VisualElement
    {
        #region VIEW ELEMENTS

        private LBSCustomVector3Field _positionField;
        private LBSCustomVector2Field _scaleField;

        private LBSCustomTextField _nameField;

        private LBSCustomButton _generateCurrLayer;
        private LBSCustomButton _generateAllLayers;

        private LBSCustomToggleField _buildLightProbes;
        private LBSCustomToggleField _bakeLights;
        private LBSCustomToggleField _replacePrev;
        private LBSCustomToggleField _reflection;

        private LBSCustomEnumField _optimizeMode;

        #endregion

        #region FIELDS
        [SerializeField, SerializeReference]
        private LBSGenerator3DSettings _settings;

        #endregion

        #region CONST

        private const string DEFAULT_NAME = "Root_name";
        private static readonly Vector2 DEFAULT_SCALE = new Vector2(2, 2);
        private static readonly Vector2 DEFAULT_RESIZE = Vector2.one;

        #endregion

        #region PROPERTIES
        public Generator3D Generator
        {
            get => LBSSettings.Instance.generator;
            set => LBSSettings.Instance.generator = value;
        }

        private static VisualTreeAsset visualTree;

        private LBSLayer Layer
        {
            get
            {
                LBSMainWindow lmw = LBSMainWindow.Instance;
                return lmw._selectedLayer;
            }
        }
        private List<LBSLayer> Layers
        {
            get
            {
                LBSMainWindow lmw = LBSMainWindow.Instance;
                List<LBSLayer> layers = new List<LBSLayer>(lmw.GetLayers());
                return layers;
            }
        }
        #endregion

        #region CONSTRUCTORS
        public Generator3DPanel()
        {
            Generator ??= new Generator3D();
            Generator = LBSSettings.Instance.generator;

            _settings ??= Generator.settings;
            visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("Generator3DPanel");
            visualTree.CloneTree(this);

            LoadVisualElement();
            PostLoad();
        }

        private void PostLoad()
        {
            _nameField.SetValueWithoutNotify(_settings.rootParentName);
            _positionField.SetValueWithoutNotify(_settings.position);
            _scaleField.SetValueWithoutNotify(_settings.scale);
            _buildLightProbes.SetValueWithoutNotify(_settings.buildLightProbes);
            _bakeLights.SetValueWithoutNotify(_settings.bakeLights);
            _replacePrev.SetValueWithoutNotify(_settings.replacePrevious);
            _reflection.SetValueWithoutNotify(_settings.reflectionProbe);
            _optimizeMode.SetValueWithoutNotify(_settings.optimization3d);
        }

        private void LoadVisualElement()
        {
            // room's name
            _nameField = this.Q<LBSCustomTextField>("ObjName");
            _nameField.RegisterValueChangedCallback(evt =>
            {
                _settings.rootParentName = evt.newValue;
                LBSSettings.Instance.MarkSettingsAsDirty();
            });
            _nameField.SetValueWithoutNotify(DEFAULT_NAME);

            _positionField = this.Q<LBSCustomVector3Field>("Position");
            _positionField.RegisterValueChangedCallback(evt =>
            {
                _settings.position = evt.newValue;
                LBSSettings.Instance.MarkSettingsAsDirty();
            });

            _scaleField = this.Q<LBSCustomVector2Field>("TileSize");
            _scaleField.RegisterValueChangedCallback(evt =>
            {
                _settings.scale = evt.newValue;
                LBSSettings.Instance.MarkSettingsAsDirty();
            });
            _scaleField.value = DEFAULT_SCALE;

            _buildLightProbes = this.Q<LBSCustomToggleField>("LightProbes");
            _buildLightProbes.RegisterValueChangedCallback(evt =>
            {
                _settings.buildLightProbes = evt.newValue;
                LBSSettings.Instance.MarkSettingsAsDirty();
            });

            _bakeLights = this.Q<LBSCustomToggleField>("BakeLights");
            _bakeLights.RegisterValueChangedCallback(evt => 
            { 
                _bakeLights.value = ToggleBakeLighting();
                _settings.bakeLights = _bakeLights.value;
            });

            _replacePrev = this.Q<LBSCustomToggleField>("Replace");
            _replacePrev.RegisterValueChangedCallback(evt =>
            {
                _settings.replacePrevious = evt.newValue;
                LBSSettings.Instance.MarkSettingsAsDirty();
            });

            _reflection = this.Q<LBSCustomToggleField>("Reflection");
            _reflection.RegisterValueChangedCallback(evt =>
            {
                _settings.reflectionProbe = evt.newValue;
            });

            _generateCurrLayer = this.Q<LBSCustomButton>("ButtonGenCurrentLayer");
            _generateCurrLayer.clicked += () =>
            {
                LBSLog generationMessage = Generator.GenerateCurrentLayer(Layer, Layers);
                LBSMainWindow.MessageNotify(generationMessage);
            };

            _generateAllLayers = this.Q<LBSCustomButton>("ButtonGenAllLayers");
            _generateAllLayers.clicked += () =>
            {

                LBSLog generationMessage = Generator.GenerateAllLayers(Layers);
                LBSMainWindow.MessageNotify(generationMessage);
            };

            _optimizeMode = this.Q<LBSCustomEnumField>("Optimization3D");
            _optimizeMode.Init(OptimizationGenMode.None);

            _optimizeMode.RegisterValueChangedCallback(evt =>
            {
                _settings.optimization3d = (OptimizationGenMode)evt.newValue;
            });

        }
        #endregion

        private bool ToggleBakeLighting()
        {
            if (_bakeLights.value == false) return false;
            else
            {
                const string storageKey = "";
                var choice = EditorUtility.DisplayDialog("Enable Bake Lighting?", "This will make 3D layer generation significantly longer. Proceed?", "OK", "Cancel", DialogOptOutDecisionType.ForThisSession, storageKey);
                return choice;
            }


        }
    }
}