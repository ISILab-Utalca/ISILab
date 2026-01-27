using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Drawers;
using ISILab.LBS.Drawers.Editor;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.VisualElements;
using ISILab.LBS.VisualElements.Editor;
using LBS.Components;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using MainView = ISILab.LBS.Plugin.UI.Editor.MainView;


namespace ISILab.LBS
{
    public class DrawManager
    {
        private readonly MainView _view = MainView.Instance;
        private LBSLevelData _level;

        public static DrawManager Instance { get; private set; }

        private readonly Dictionary<(Type, LBSLayer), Drawer> _drawerCache = new();
        private readonly Dictionary<LBSLayer, bool> _preVisibility = new();
        private Dictionary<VisualElement, PickingMode> elementPicks = new();
        
        public DrawManager()
        {
            Instance = this;
        }

        public void AddContainer(LBSLayer layer)
        {
            _view.AddContainer(layer);
        }

        public void RemoveContainer(LBSLayer layer)
        {
            _view.RemoveContainer(layer);
        }

        public static void ReDraw()
        {
            Instance.RedrawLevel(Instance._level);
        }

        private void DrawLayer(LBSLayer layer)
        {
            // Validation
            if (layer == null) return;

            UpdateVisibility(layer);
            
            // Draw behaviours and assistants (if both share same drawer system)
            DrawVisibleComponents(layer.Behaviours, layer);
            DrawVisibleComponents(layer.Assistants, layer);
        }

        public void UpdateLayer(LBSLayer layer)
        {
            // Validation
            if (layer == null) return;

            _view.ClearLayerContainer(layer);
            UpdateVisibility(layer);

            UpdateVisibleComponents(layer.Behaviours, layer);
            UpdateVisibleComponents(layer.Assistants, layer);
        }

        private void UpdateVisibility(LBSLayer layer)
        {
            if (!_preVisibility.ContainsKey(layer))
            {
                _preVisibility.Add(layer, layer.IsVisible);
            }

            // Change visibility of layer
            else
            {
                switch (layer.IsVisible)
                {
                    case true when !_preVisibility[layer]:
                        ShowVisuals(layer.Assistants, layer);
                        ShowVisuals(layer.Behaviours, layer);
                        break;
                    case false when _preVisibility[layer]:
                        HideVisuals(layer.Assistants, layer);
                        HideVisuals(layer.Behaviours, layer);
                        break;
                }
            }
            _preVisibility[layer] = layer.IsVisible;
        }

        private void DrawVisibleComponents<T>(List<T> components, LBSLayer layer)
        {
            foreach (T component in components)
            {
                if (component == null)continue;
                Drawer drawer = GetOrCreateDrawer(component.GetType(), layer);
                if(drawer == null) continue;

                drawer.Draw(component, MainView.Instance, layer.TileSize);
            }
        }

        private void UpdateVisibleComponents<T>(List<T> components, LBSLayer layer)
        {
            foreach (T component in components)
            {
                if (component == null) continue;
                Drawer drawer = GetOrCreateDrawer(component.GetType(), layer);
                if (drawer == null) continue;
                layer.GetBehaviour<LBSBehaviour>()?.CheckKeys();
                drawer.Update(component, MainView.Instance, layer.TileSize);
            }
        }

        public void DrawSingleComponent<T>(T component, LBSLayer layer)
        {
            if (component == null) return;
            Drawer drawer = GetOrCreateDrawer(component.GetType(), layer);
            if (drawer == null) return;
            drawer.Draw(component, MainView.Instance, layer.TileSize);
        }

        private void HideVisuals<T>(List<T> components, LBSLayer layer)
        {
            foreach (T component in components)
            {
                if (component == null)continue;
                Drawer drawer = GetOrCreateDrawer(component.GetType(), layer);
                drawer?.HideVisuals(component, _view);
            }
        }
        private void ShowVisuals<T>(List<T> components, LBSLayer layer)
        {
            foreach (T component in components)
            {
                if (component == null)continue;
                Drawer drawer = GetOrCreateDrawer(component.GetType(), layer);
                drawer?.ShowVisuals(component, _view);
            }
        }

        private Drawer GetOrCreateDrawer(Type type, LBSLayer layer)
        {
            (Type type, LBSLayer layer) pairKey = (type, layer);
            if (_drawerCache.TryGetValue(pairKey, out Drawer drawer)) return drawer;
            
            Type drawerType = LBS_Editor.GetDrawer(type);
            if (drawerType == null)
                return null;

            drawer = Activator.CreateInstance(drawerType) as Drawer;
            if (drawer != null)
                _drawerCache[pairKey] = drawer;
            return drawer;
        }
        
        private List<Drawer> GetLayerDrawers(LBSLayer layer)
        {
            return _drawerCache.Where(kvp => kvp.Key.Item2.Equals(layer)).Select(kvp => kvp.Value).ToList();
        }

        public void RedrawLayer(LBSLayer layer)
        {
            _view.ClearLayerContainer(layer);
            DrawLayer(layer);
        }

        public void RedrawLevel(LBSLevelData level, bool deepClean = false)
        {
            UnityEngine.Debug.Log("Redraw Level");
            var newDrawers = GetNewDrawers();
            foreach (LBSLayer layer in level.Layers)
            {
                bool preVisible = _preVisibility.ContainsKey(layer) ? _preVisibility[layer] : layer.IsVisible;
                //if(deepClean)

                GetLayerDrawers(layer).ForEach(drawer => 
                { 
                    drawer.FullRedrawRequested = preVisible && layer.IsVisible;

                    if (layer.IsVisible)
                    {
                        if (!newDrawers.Contains(drawer.GetType())) // Temporary condition while working on other drawers
                        {
                            _view.ClearLayerContainer(layer, deepClean || (preVisible && layer.IsVisible));
                        }
                        else
                            _view.ClearLayerContainer(layer, deepClean);
                    }
                });

                //if (!layer.IsVisible) continue;
                //_view.ClearLayerContainer(layer, deepClean);
            }
            DrawLevel(level);

            List<Type> GetNewDrawers() => new List<Type>() { typeof(ExteriorDrawer), typeof(SchemaDrawer), typeof(PopulationDrawer), typeof(PopulationTileDrawer), typeof(QuestGraphDrawer), typeof (QuestNodeBehaviourDrawer), typeof (NoteDrawer) };
        }

        private void DrawLevel(LBSLevelData level)
        {
            _level = level;
            foreach (LBSLayer layer in level.Layers)
            {
                DrawLayer(layer);
            }
        }

        public void ChangeOpacityAll(float opacity)
        {
            var layers = LBSMainWindow.Instance.GetLayers();
            if (!layers.Any()) return;
            
            foreach (LBSLayer layer in layers)
            {
              ChangeLayerOpacity(layer, opacity);
            }
        }

        public static void ChangeLayerOpacity(LBSLayer layer, float opacity)
        {
            if(layer is null)return;
            var elements =  MainView.Instance.GetAllElementsInLayer(layer);
            if(!elements.Any()) return;
            foreach (GraphElement element in elements)
            {
                element.style.opacity = opacity;
            }
        }

        /// <summary>
        /// Disables the picking mode of all the existing elements but the exception.
        /// Stores the picking mode of each existing element to restore afterwards
        /// </summary>
        /// <param name="newPickingMode">the picking mode that will be assigned to all visual elements</param>
        /// <param name="exceptions">the visual elements that wont be affected</param>
        public void PickingModeChangeAll(PickingMode newPickingMode, List<VisualElement> exceptions)
        {
            // restore all before changing again to avoid rewriting the same pickmode(changing original)
            Instance.PickingModeRestoreAll();
            
            Dictionary<VisualElement, PickingMode> exceptionsMap = new Dictionary<VisualElement, PickingMode>();
            foreach (VisualElement ve in exceptions)
            {
                exceptionsMap.TryAdd(ve, ve.pickingMode);
            }
            
            foreach (LBSLayer layer in LBSMainWindow.Instance.GetLayers())
            {
                foreach(GraphElement element in _view.GetAllElementsInLayer(layer))
                {
                    if (exceptions.Contains(element))
                    {
                        continue;
                    }
                    ChangePickingMode(element, newPickingMode, exceptions);
                }
            }

            // make sure to to keep pick mode
            foreach (VisualElement ve in exceptions)
            {
                ve.pickingMode = exceptionsMap[ve];
            }
        }

        private void ChangePickingMode(VisualElement element, PickingMode newPickingMode, List<VisualElement> exceptions)
        {
            if (element == null)
            {
                return;
            }
            if (exceptions.Contains(element))
            {
                return;
            }
            
            if (!elementPicks.ContainsKey(element)) elementPicks[element] = element.pickingMode;
            
            //Debug.Log("pick-mode changed to " + newPickingMode);
            
            // Recursively apply to parent}
            foreach (VisualElement child in element.Children())
            {
                ChangePickingMode(child, newPickingMode, exceptions);
            }
        }

        /// <summary>
        /// Restores the picking mode of all elements if they were stored using ChangePickingModeAll
        /// </summary>
        public void PickingModeRestoreAll()
        {
            foreach (var elementPick in elementPicks)
            {
                elementPick.Key.pickingMode = elementPick.Value;
            }
            
            //Debug.Log("restore pickmode all");
            elementPicks.Clear();
        }
    }
}
