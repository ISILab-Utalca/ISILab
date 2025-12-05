
using ISILab.LBS.Characteristics;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.VisualElements;
using LBS.Bundles;
using PathOS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.Bundles.Editor
{
    [CustomEditor(typeof(Bundle)), CanEditMultipleObjects]
    public class BundleEditor : UnityEditor.Editor
    {
        ListView assets;
        ListView characteristics;
        ListView childBundles;

        Action OnAnyCharacteristicChanged;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Event.current.commandName == "UndoRedoPerformed")
            {
                this.Repaint();
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            //InspectorElement.FillDefaultInspector(root, this.serializedObject, this);
            var bundle = target as Bundle;

            //Erase empty children
            if (bundle != null)
            {
                bundle.RemoveNullChildren();
            }

            #region COMMONS
            MakeTitle("Common Settings");

            SerializeProperty("layerContentFlags", prop =>
            {
                Selection.activeObject = null;
                EditorApplication.delayCall += () => EditorApplication.delayCall += () => Selection.activeObject = bundle;
            });

            SerializeProperties("", "bundleName", "color");
            SerializeProperty("icon", prop =>
            {
                var bundle = target as Bundle;
                if (bundle == null) return;

                // Extract the new VectorImage reference from the serialized property
                var newIcon = prop.objectReferenceValue as VectorImage;

                // Use your custom setter instead of modifying the field directly
                bundle.Icon = newIcon;

                // Mark asset dirty to persist the change
                EditorUtility.SetDirty(bundle);
                root.MarkDirtyRepaint();

                Debug.Log($"[BundleEditor] Icon updated for '{bundle.name}' → {newIcon?.name ?? "null"}");
            });

            #endregion

            bool hasSpecificSettings = false;
            bool interiorSettings   = false, 
                 exteriorSettings   = false,
                 populationSettings = false,
                 questSettings      = false,
                 simulationSettings = false;
            if(bundle != null && bundle.ChildsBundles.Count == 0)
            {
                interiorSettings    = (bundle.LayerContentFlags & BundleFlags.Interior  ) == BundleFlags.Interior;
                exteriorSettings    = (bundle.LayerContentFlags & BundleFlags.Exterior  ) == BundleFlags.Exterior;
                populationSettings  = (bundle.LayerContentFlags & BundleFlags.Population) == BundleFlags.Population;
                questSettings       = (bundle.LayerContentFlags & BundleFlags.Quest     ) == BundleFlags.Quest;
                simulationSettings  = (bundle.LayerContentFlags & BundleFlags.Simulation) == BundleFlags.Simulation;

                exteriorSettings = false; // (!!) Remove line if an exterior specific property is created
                questSettings = false; // (!!) Remove line if a quest specific property is created

                hasSpecificSettings = interiorSettings || exteriorSettings || populationSettings || questSettings || simulationSettings;
            }

            if (hasSpecificSettings)
            {
                SerializeProperties("");
                MakeTitle("Specific Settings");
            }

            if (interiorSettings)
                SerializeProperty("anchorPosition");

            if (exteriorSettings)
                ;

            if (populationSettings)
                SerializeProperties("elementFlag", "tileSize");

            if (questSettings)
                ;

            if (simulationSettings)
            {
                SerializeProperty("entityType", prop =>
                {
                    if (bundle.EntityType != EntityType.ET_NONE && !bundle.AdmissibleEntityTypes.Contains(bundle.EntityType))
                        bundle.AdmissibleEntityTypes.Insert(0, bundle.EntityType);
                });
                SerializeProperty("admissibleTypes");
            }


            SerializeProperties("", "assets");

            #region Characteristics

            characteristics = new ListView();
            characteristics.headerTitle = "Characteristics";
            characteristics.showAddRemoveFooter = true;
            characteristics.showBorder = true;
            characteristics.showFoldoutHeader = true;
            characteristics.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            characteristics.makeItem = MakeItem;
            characteristics.bindItem = BindItem;
            characteristics.unbindItem = UnbindItem;

            characteristics.itemsSource = bundle!.Characteristics;

            characteristics.itemsAdded += items =>
            {
                var x = bundle!.Characteristics.Last();
                bundle.Characteristics.RemoveAt(bundle.Characteristics.Count - 1);

                EditorGUI.BeginChangeCheck();
                Undo.RegisterCompleteObjectUndo(bundle, "Add characteristics");
                bundle.Characteristics.Add(x);

                if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(target);
                Undo.undoRedoPerformed += UNDO;

                characteristics.RefreshItem(items.First());
            };
            characteristics.itemsRemoved += items =>
            {
                foreach(int index in items)
                {
                    bundle.RemoveCharacteristicCallback(characteristics.itemsSource[index] as LBSCharacteristic);
                }
                EditorApplication.delayCall += () => OnAnyCharacteristicChanged?.Invoke();
            };

            root.Insert(root.childCount, characteristics);

            foreach (var characteristic in bundle!.Characteristics)
            {
                characteristic?.Init(bundle);
            }

            #endregion

            #region Child Bundles

            var lv = root.Children().ToList()[5] as PropertyField;
            lv.TrackPropertyValue(serializedObject.FindProperty("characteristics"), (sp) =>
            {
         
                //foreach (var child in bundle.Characteristics) Debug.Log(child);
                
                characteristics.RefreshItems();
                Repaint();
            });

            // Create ListView for Child Bundles
            childBundles = new ListView();
            childBundles.headerTitle = "Child Bundles";
            childBundles.reorderable = false;
            childBundles.showAddRemoveFooter = false;
            childBundles.showBorder = true;
            childBundles.showFoldoutHeader = true;
            childBundles.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            childBundles.makeItem = MakeChildBundleItem;
            childBundles.bindItem = BindChildBundleItem;

            childBundles.itemsSource = bundle.ChildsBundles;
            
            
            childBundles.itemsAdded += view =>
            {
                if (bundle.ChildsBundles.Count == 0) return;
                var x = bundle.ChildsBundles.Last();
                bundle.ChildsBundles.RemoveAt(bundle.ChildsBundles.Count - 1);
                
                EditorGUI.BeginChangeCheck();
                Undo.RegisterCompleteObjectUndo(bundle, "Add child bundle");
                bundle.AddChild(x);

                if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(target);
                Undo.undoRedoPerformed += UNDO;
            };
            
            childBundles.itemsRemoved += (view) =>
            {
                if(bundle.ChildsBundles.Count == 0) return;
                
                var x = bundle.ChildsBundles.Last();
                // Remove the child bundle from the parent bundle's child list
                if (bundle.ChildsBundles.Contains(x))
                {
                    EditorGUI.BeginChangeCheck(); 
                    Undo.RegisterCompleteObjectUndo(bundle, "Remove child bundle");
                    bundle.RemoveChild(x);

                    if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(target);
                    Undo.undoRedoPerformed += UNDO;
                }
            };
            
            root.Insert(root.childCount, childBundles);
            
            var addButton = new Button(() =>
            {
                EditorGUI.BeginChangeCheck();
                
                ShowAddChildBundleMenu(bundle);
                if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(target);
                Undo.undoRedoPerformed += UNDO;
                
            }){ text = "Add Child Bundle" };
            
            
            var removeButton = new Button(() =>
            {
                EditorGUI.BeginChangeCheck();
                
                ShowRemoveChildBundle(bundle);
                if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(target);
                Undo.undoRedoPerformed += UNDO;
                
            }){ text = "Remove Child Bundle"};
            
            
            var cblv = root.Children().ToList()[6] as PropertyField;
            cblv.TrackPropertyValue(serializedObject.FindProperty("childsBundles"), (sp) =>
            {
                //foreach (var child in bundle.ChildsBundles) Debug.Log(child);
                
                childBundles.itemsSource = bundle.ChildsBundles;
                characteristics.RefreshItems();
                childBundles.RefreshItems();
                Repaint();
            });
            
            
            VisualElement buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row; 
            buttonContainer.style.alignItems = Align.Auto;
            buttonContainer.style.justifyContent = Justify.Center; 
            
            buttonContainer.Add(addButton);
            buttonContainer.Add(removeButton);
            
            root.Add(buttonContainer);
            
            #endregion
            
            return root;

            void SerializeProperty(string name, Action<SerializedProperty> trackValueCallback = null)
            {
                SerializedProperty property = serializedObject.FindProperty(name);
                if(property is not null)
                {
                    PropertyField field = new PropertyField(property);
                    field.Bind(serializedObject);
                    root.Add(field);

                    if(trackValueCallback != null)
                    {
                        field.TrackPropertyValue(property, trackValueCallback);
                    }
                }
            }

            void SerializeProperties(params string[] names)
            {
                for(int i = 0; i < names.Length; i++)
                {
                    if (string.IsNullOrEmpty(names[i]))
                    {
                        var v = new VisualElement();
                        v.style.height = 12;
                        root.Add(v);
                    }
                    else
                    {
                        SerializeProperty(names[i]);
                    }
                }
            }

            void MakeTitle(string title)
            {
                var newTitle = new Label(title);
                newTitle.style.fontSize = 13;
                newTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                newTitle.style.paddingBottom = newTitle.style.paddingTop = 5;
                root.Add(newTitle);
            }
        }
        
        private void ShowRemoveChildBundle(Bundle bundle)
        {
            var allBundles = bundle.ChildsBundles;
            
            GenericMenu menu = new GenericMenu();
            
            // Add existing child bundles to the menu
            foreach (var potentialChild in allBundles)
            {
                if (potentialChild != null)
                {
                    menu.AddItem(new GUIContent(potentialChild.name), false, () =>
                    {
                        // Add selected bundle to the child bundles list
                        if (MakeChildBundleItem() is ObjectField cb)
                        {
                            EditorGUI.BeginChangeCheck(); 
                            Undo.RegisterCompleteObjectUndo(bundle, "Remove child bundle");
                            //  cb.SetValueWithoutNotify(potentialChild);
                            bundle.RemoveChild(potentialChild);
                        
                            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(target);
                            Undo.undoRedoPerformed += UNDO;
                        }
                  
                    });
                }
            }
            menu.ShowAsContext();
        }
        
        private void ShowAddChildBundleMenu(Bundle bundle)
        {
            // Get all available bundles in the project
            var allBundles = AssetDatabase.FindAssets("t:Bundle")
                .Select(guid => AssetDatabase.LoadAssetAtPath<Bundle>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();

            GenericMenu menu = new GenericMenu();
            
            // Add valid child bundles to the menu
            foreach (var potentialChild in allBundles)
            {
                if (!bundle.IsBundleValidChild(potentialChild))  continue;
                menu.AddItem(new GUIContent(potentialChild.name), false, () =>
                {
                    // Add selected bundle to the child bundles list
                    if (MakeChildBundleItem() is ObjectField cb)
                    {
                        EditorGUI.BeginChangeCheck(); 
                        Undo.RegisterCompleteObjectUndo(bundle, "Add child bundle");
                        cb.SetValueWithoutNotify(potentialChild);
                        bundle.AddChild(potentialChild);
                        
                        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(target);
                        Undo.undoRedoPerformed += UNDO;
                    }
                  
                });
            }

            menu.ShowAsContext();
        }
        
        private void UNDO()
        {
            characteristics.Rebuild();
            childBundles.Rebuild();
            Undo.undoRedoPerformed -= UNDO;
        }

        bool CharacteristicUniquenessFilter(object obj)
        {
            if (obj is not Type type) return false;
            if (type.BaseType != typeof(LBSCharacteristic)) return false;

            if (LBSCharacteristic.IsUnique(type) && (target as Bundle).HasCharacteristic(type))//Characteristics.Any(ch => ch?.GetType() == type))
                return false;
            return true;
        }

        bool CharacteristicExclusivenessFilter(object obj)
        {
            if (obj is not Type type) return false;
            if (type.BaseType != typeof(LBSCharacteristic)) return false;

            if(LBSCharacteristic.IsExclusive(type, out List<List<Type>> exclusiveGroups) && exclusiveGroups.Any(group => group.Any(t => (target as Bundle).HasCharacteristic(t))))
                return false;
            return true;
        }

        

        VisualElement MakeItem()
        {
            var bundle = target as Bundle;
            var v = new DynamicFoldout(typeof(LBSCharacteristic), CharacteristicUniquenessFilter, CharacteristicExclusivenessFilter);
            return v;
        }

        void BindItem(VisualElement ve, int index)
        {
            //Debug.Log($"Bind ({index})");
            var bundle = target as Bundle;
            if (index < bundle!.Characteristics.Count)
            {
                var cf = ve.Q<DynamicFoldout>();
                cf.Label = "Characteristic " + index + ":";
                if (bundle.Characteristics[index] != null)
                {
                    cf.Data = bundle.Characteristics[index];
                    bundle.Characteristics[index].Init(bundle);
                }

                OnAnyCharacteristicChanged -= cf.UpdateDropdown;
                OnAnyCharacteristicChanged += cf.UpdateDropdown;

                cf.OnChoiceSelection = () =>
                {
                    bundle.Characteristics[index] = cf.Data as LBSCharacteristic;
                    (cf.Data as LBSCharacteristic)?.Init(bundle);
                    OnAnyCharacteristicChanged?.Invoke();
                };
            }
        }

        void UnbindItem(VisualElement ve, int index)
        {
            //Debug.Log($"Unbind ({index})");
            var cf = ve.Q<DynamicFoldout>();
            OnAnyCharacteristicChanged -= cf.UpdateDropdown;
        }

        VisualElement MakeChildBundleItem()
        {
            var bundle = target as Bundle;
            var v = new ObjectField();
            v.objectType = typeof(Bundle);
            v.SetEnabled(false);
            v.style.opacity = 100;
            v.RegisterValueChangedCallback(HandleChildBundleChange);
            
            return v;
        }

        private void BindChildBundleItem(VisualElement ve, int index)
        {
            var bundle = target as Bundle;
            if (index < bundle!.ChildsBundles.Count)
            {
                var cb = ve.Q<ObjectField>();
                cb.objectType = typeof(Bundle);
                
                if (bundle.ChildsBundles[index] != null)
                {
                    cb.value = bundle.ChildsBundles[index];
                   // bundle.ChildsBundles[index].Reload();
                }

                //cb.RegisterValueChangedCallback(HandleChildBundleChange);
            }
        }

        private void HandleChildBundleChange(ChangeEvent<UnityEngine.Object> evt)
        {
            var parent = target as Bundle;
            if (parent == null) return;
            
            var newBundle = evt.newValue as Bundle;
            if (newBundle == null) return;

            if (!parent.IsBundleValidChild(newBundle)) return;
            
            parent.AddChild(newBundle);
        }
        
        public void Save()
        {
            EditorUtility.SetDirty(target);
        }

        private void OnDisable()
        {
            if (target != null) EditorUtility.SetDirty(target);
        }

        private void OnDestroy()
        {
            if (target != null) EditorUtility.SetDirty(target);
        }

    }
}