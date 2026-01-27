using ISILab.Commons.Utility.Editor;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Manipulators;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.VisualElements.Editor.CustomComponents.Interfaces;
using LBS;
using LBS.Components;
using LBS.VisualElements;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Macros;

namespace ISILab.LBS.VisualElements
{
    [LBSCustomEditor("PopulationBehaviour", typeof(PopulationBehaviour))]
    public class PopulationBehaviourEditor : LBSCustomEditor, IToolProvider, IBundleFilter
    {
        #region FIELDS

        private PopulationBehaviour behaviour;

        private Dictionary<string, List<Bundle.EElementFlag>> displayChoices = new();
        private Bundle _mainBundle;
        private DropdownField type;

        private AddPopulationTile addPopulationTile;
        private RemovePopulationTile removePopulationTile;
        private RotatePopulationTile rotatePopulationTile;
        private MovePopulationTile movePopulationTile;
        
        #region VIEW FIELDS
        private readonly Color BHcolor = LBSSettings.Instance.view.behavioursColor;
        private VectorImage icon = LBSAssetMacro.LoadAssetByGuid<VectorImage>("87f2bb6f2c78b184a8ea2b6a5b14f878");
        private SimplePallete bundlePallete;
        private WarningPanel warningPanel;

        public LBSButtonListFilter BundlePickerWindow { get; set; }
        #endregion

        #endregion

        #region CONSTRUCTORS
        public PopulationBehaviourEditor(object target) : base(target)
        {

            behaviour = target as PopulationBehaviour;
            if (behaviour is null) return;
            //_collection = load default collection
            
            List<Bundle.EElementFlag> characterList = new List<Bundle.EElementFlag> { Bundle.EElementFlag.Character };
            List<Bundle.EElementFlag> itemList = new List<Bundle.EElementFlag> { Bundle.EElementFlag.Item };
            List<Bundle.EElementFlag> interactableList = new List<Bundle.EElementFlag> { Bundle.EElementFlag.Interactable };
            List<Bundle.EElementFlag> areaList = new List<Bundle.EElementFlag> { Bundle.EElementFlag.Trigger };
            List<Bundle.EElementFlag> propList = new List<Bundle.EElementFlag> { Bundle.EElementFlag.Prop };
            List<Bundle.EElementFlag> miscList = new List<Bundle.EElementFlag> { Bundle.EElementFlag.Misc };
            List<Bundle.EElementFlag> allList = new List<Bundle.EElementFlag>
            {
                Bundle.EElementFlag.Misc,
                Bundle.EElementFlag.Prop,
                Bundle.EElementFlag.Trigger,
                Bundle.EElementFlag.Interactable,
                Bundle.EElementFlag.Item,
                Bundle.EElementFlag.Character
            };
            
            _mainBundle = behaviour.MainBundle;
            behaviour.SelectedFilter = behaviour.allFilter;
            displayChoices.Add(behaviour.allFilter, allList);
            displayChoices.Add(nameof(Bundle.EElementFlag.Character), characterList);
            displayChoices.Add(nameof(Bundle.EElementFlag.Item), itemList);
            displayChoices.Add(nameof(Bundle.EElementFlag.Interactable), interactableList);
            displayChoices.Add(nameof(Bundle.EElementFlag.Trigger), areaList);
            displayChoices.Add(nameof(Bundle.EElementFlag.Prop), propList);
            displayChoices.Add(nameof(Bundle.EElementFlag.Misc), miscList);

            SetInfo(behaviour);
            CreateVisualElement();
        }
        #endregion

        #region METHODS
        public sealed override void SetInfo(object paramTarget)
        {

            behaviour = paramTarget as PopulationBehaviour;
            if (behaviour == null) return;

            _mainBundle = behaviour.MainBundle;
            behaviour.OwnerLayer.OnChange += () =>
            {
                //PopulationTileView.SelectedTile?.Highlight(false, true);
            };
        }

        public void SetTools(ToolKit toolkit)
        {
            addPopulationTile = new AddPopulationTile();
            var t1 = new LBSTool(addPopulationTile);

            removePopulationTile = new RemovePopulationTile();
            var t2 = new LBSTool(removePopulationTile);
            
            rotatePopulationTile = new RotatePopulationTile();
            var t3 = new LBSTool(rotatePopulationTile);

            movePopulationTile = new MovePopulationTile();
            var t4 = new LBSTool(movePopulationTile);
            
            addPopulationTile.SetRemover(removePopulationTile);

            foreach (LBSTool tool in new[] { t1, t2, t3, t4 })
            {
                tool.OnSelect += LBSInspectorPanel.ActivateBehaviourTab;
                toolkit.ActivateTool(tool, behaviour.OwnerLayer, behaviour);
            }

        }

        protected sealed override VisualElement CreateVisualElement()
        {

            VisualTreeAsset visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PopulationBehaviourEditor");
            visualTree.CloneTree(this);
            
            // WarningPanel
            warningPanel = this.Q<WarningPanel>();

            //var collectionField = this.Q<ObjectField>("BundleCollection");
            //// only updates the first bundle value change - fix pending
            //collectionField.RegisterValueChangedCallback(evt =>
            //{
            //    var collection = evt.newValue as BundleCollection;
            //    collectionField.value = collection;
            //    SetCollection(collection);
            //    UpdateElementBundles();
            //    
            //});

            TileRotatorEditor tileRotatorEditor = this.Q<TileRotatorEditor>("TileRotatorEditor");
            tileRotatorEditor.SetInfo(behaviour);

            LBSCustomObjectField bundleField = this.Q<LBSCustomObjectField>("BundleField");
            bundleField.RegisterValueChangedCallback(evt =>
            {
                var bundle = evt.newValue as Bundle;
                bundleField.value = bundle;
                SetBundle(bundle);
                UpdateElementBundles();
            });
            bundleField.UseCustomFilter = true;
            bundleField.CustomFilter = pick =>
            {
                List<Bundle> bundles = BundleQueryUtility.FindBundlesWithCharacteristic<LBSMainPopulationBundle>(includeChildren: true);
                (this as IBundleFilter).OpenFilterWindow(bundles, picked => pick(picked));
            };
            
            type =  this.Q<DropdownField>("Type");
            type.choices = displayChoices.Keys.ToArray().ToList();
            type.RegisterValueChangedCallback(evt =>
            {
                string filter = evt.newValue;
                behaviour.selectedTypeFilter = filter; 
                UpdateElementBundles();
            });

            type.SetValueWithoutNotify(behaviour.SelectedFilter); 
            
            
            bundlePallete = this.Q<SimplePallete>("ConnectionPallete");
            bundlePallete.DisplayAddElement = false;
            UpdateElementBundles();
            SetPallete();
            bundlePallete.Repaint();
            
            //collectionField.SetValueWithoutNotify(behaviour.BundleCollection);
            bundleField.SetValueWithoutNotify(behaviour.MainBundle);
            
            MarkDirtyRepaint();
            
            return this;
        }

        private void SetPallete()
        {
            // Set init options
            bundlePallete.ShowGroups = false;
            bundlePallete.ShowAddButton = false;
            bundlePallete.ShowRemoveButton = false;
            bundlePallete.ShowNoElement = false;
            
            bundlePallete.Repaint();
            
            bundlePallete.OnSelectOption += (selected) =>
            {
                behaviour.selectedToSet = selected as Bundle;
                behaviour.MainBundle = _mainBundle;
             
                ToolKit.Instance.SetActive(typeof(AddPopulationTile));
            };
            
            bundlePallete.OnSetTooltip += (option) =>
            {
                Bundle b = option as Bundle;

                string tooltip = "Tags:";

                List<string> tags = LBSAssetMacro.GetAllTagNames(b);
                if (tags.Count > 0)
                {
                    tags.ForEach(t => tooltip += "\n- " + t);
                    //b.Characteristics.ForEach(c => tooltip += "\n- " + c?.ToString());//.GetType());
                }
                else
                {
                    tooltip += "\n[None]";
                }
                return tooltip;
            };

            bundlePallete.OnRepaint += () =>
            {
                bundlePallete.Selected = behaviour.selectedToSet;
                //bundlePallete.CollectionSelected = behaviour.BundleCollection;
            };

            bundlePallete.SetName("Entities");
            bundlePallete.SetIcon(icon, BHcolor);

        }

        private void UpdateElementBundles()
        {
            //if (_collection == null)
            if(_mainBundle == null)
            {
                warningPanel.SetDisplay(true);
                bundlePallete.DisplayContent(false);
                return;
            }
            
            type.SetValueWithoutNotify(behaviour.SelectedFilter); 
            warningPanel.SetDisplay(false);
            bundlePallete.DisplayContent(true);
            //List<Bundle> bundles = _collection.Collection;
            List<Bundle> bundles = _mainBundle.ChildsBundles;
            List<Bundle> candidates = new();
            if (type.value == behaviour.allFilter)
            {
                candidates = bundles
                    .Where(b => b.LayerContentFlags.HasFlag(BundleFlags.Population)).ToList();
            }
            else
            {
                Bundle.EElementFlag filter = displayChoices[type.value][0];
                candidates = bundles
                    .Where(b => b.LayerContentFlags.HasFlag(BundleFlags.Population) && b.HasAnyFlag(filter)) // get the bundle type at the filter index
                    .ToList();
            }
            bundlePallete.ShowGroups = false;
            
            candidates.Sort((b1, b2) => b1.BundleName.CompareTo(b2.BundleName));
            object[] options = new object[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                options[i] = candidates[i];
            }
         
            // Init options
            bundlePallete.SetOptions(options, (optionView, option) =>
            {
                var bundle = (Bundle)option;
                optionView.Label = bundle.BundleName;
                optionView.FrameColor = bundle.Color;
                optionView.Icon = bundle.Icon;
               // var size = 20f;

                //optionView.style.width = size;
              //  optionView.style.height = size * 1.75f;
            });
            
            // Save current selected options in layer
            behaviour.MainBundle = _mainBundle;
            
            bundlePallete.Repaint();
        }

        private void SetBundle(Bundle bundle)
        {
            behaviour.MainBundle = bundle;
            _mainBundle = bundle;
        }

        public override void OnUnfocus()
        {
            base.OnUnfocus();
            (this as IBundleFilter).CloseFilterWindow();
        }

        #endregion
    }
}