using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.VisualElements;
using LBS.Components;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Plugin.UI.Editor
{

    /// <summary>
    /// Main canvas view used in the editor graph system. Handles layers, manipulators, and visual elements.
    /// </summary>
    [UxmlElement]
    public partial class MainView : GraphView 
    {

        #region SINGLETON
        public static MainView Instance { get; private set; }

        /// <summary>
        /// Converts world position to local canvas space by accounting for viewport transform and zoom.
        /// </summary>
        public Vector2 FixPos(Vector2 v)
        {
            var t = new Vector2(this.viewTransform.position.x, this.viewTransform.position.y);
            var newPos = (v - t) / this.scale;
            return newPos;
        }
        #endregion

        #region FIELDS
        private ExternalBounds _bound;
        private readonly List<Manipulator> _manipulators = new();

        private readonly LayerContainer _defaultLayer = new();

        [SerializeReference]
        private readonly Dictionary<LBSLayer, LayerContainer> _layers = new();
        
        // shared manipulators such as drag, zoom
        private List<Manipulator> _defaultManipulators = new();
        private ContentZoomer _zoomer;
        private ContentDragger _cDragger;
        private SelectionDragger _sDragger;
        private bool _zoomEnabled = true;

        #endregion

        #region EVENTS
        public event Action OnClearSelection;
        #endregion

        #region CONSTRUCTORS
        public MainView()
        {
            Insert(0, new GridBackground());
            
            //Style part
            StyleSheet styleSheet = DirectoryTools.GetAssetByName<StyleSheet>("MainViewUSS");
            styleSheets.Add(styleSheet);
            AddToClassList("lbs-graph-view");
            RemoveFromClassList("graphView");
            
            style.flexGrow = 1;
            
            SetBasicManipulators();
            InitBound(20000, int.MaxValue / 2);

            AddElement(_bound);

            // Singleton
            if (Instance != this) Instance = this;
                
            
            AddManipulator(new ContextualMenuManipulator((evt) =>
            {
                // Prevent the default right-click menu
              //  evt.StopPropagation();
                evt.menu.ClearItems(); 
                
            }));
        }

        #endregion

        #region INTERNAL_METHODS

        /// <summary>
        /// Initializes interior and exterior boundary boxes for canvas scrolling limits.
        /// </summary>
        private void InitBound(int interior, int exterior)
        {
            this._bound = new ExternalBounds(
                new Rect(
                    new Vector2(-interior, -interior),
                    new Vector2(interior * 2, interior * 2)
                    ),
                new Rect(
                    new Vector2(-exterior, -exterior),
                    new Vector2(exterior * 2, exterior * 2)
                    )
                );
        }
        #endregion

        #region METHODS_MANIPULATORS

        /// <summary>
        /// Sets up default manipulators for zoom, dragging, and selection. These are always accessible
        /// from any layers and behaviours or assistants!
        /// </summary>
        private void SetBasicManipulators()
        {
            var setting = LBSSettings.Instance.general;

            _zoomer = new ContentZoomer();

            setting.OnChangeZoomValue = (min, max) =>
            {
                _zoomer.maxScale = setting.zoomMax;
                _zoomer.minScale = setting.zoomMin;
            };

            _zoomer.maxScale = setting.zoomMax;
            _zoomer.minScale = setting.zoomMin;

            RegisterCallback<WheelEvent>(evt => { _zoomer.target = !_zoomEnabled ? null : this; });
            
            _cDragger = new ContentDragger();
            _sDragger = new SelectionDragger();

            var manipulators = new List<Manipulator>() { _zoomer, _cDragger, _sDragger };
            _defaultManipulators = manipulators;
            SetManipulators(manipulators);
        }

        /// <summary>
        /// Clears selected items and invokes the clear selection event.
        /// </summary>
        public override void ClearSelection() // (?)
        {
            base.ClearSelection();
            if (selection.Count == 0)
            {
                OnClearSelection?.Invoke();
            }
        }

        /// <summary>
        /// Sets a single manipulator. May keep the default manipulators.
        /// </summary>
        public void SetManipulator(Manipulator current, bool keepDefaults = false)
        {
            ClearManipulators();
            if(keepDefaults) AddManipulators(_defaultManipulators);
            AddManipulator(current);
        }

        private void SetManipulators(List<Manipulator> manipulators)
        {
            ClearManipulators();
            AddManipulators(manipulators);
        }

        private void ClearManipulators()
        {
            foreach (var m in this._manipulators)
            {
                this.RemoveManipulator((IManipulator)m);
            }
            this._manipulators.Clear();
        }

        public void RemoveManipulator(Manipulator manipulator)
        {
            this._manipulators.Remove(manipulator);
            this.RemoveManipulator(manipulator as IManipulator);
        }

        public void RemoveManipulators(List<Manipulator> manipulators)
        {
            foreach (var m in manipulators)
            {
                this._manipulators.Remove(m);
                this.RemoveManipulator((IManipulator)m);
            }
        }

        public void AddManipulator(Manipulator manipulator)
        {
            if (_manipulators.Contains(manipulator)) return;
            this._manipulators.Add(manipulator);
            this.AddManipulator(manipulator as IManipulator);
        }

        private void AddManipulators(List<Manipulator> manipulators)
        {
            foreach (var m in manipulators)
            {
                if (!this._manipulators.Contains(m))
                {
                    this._manipulators.Add(m);
                    this.AddManipulator((IManipulator)m);
                }
            }
        }
        
        /// <summary>
        /// Returns true if a manipulator of type T exists in the canvas.
        /// </summary>
        public bool HasManipulator<T>() where T : Manipulator
        {
            return _manipulators.Any(m => m is T);
        }

        /// <summary>
        /// Enables or disables zooming via the mouse wheel.
        /// </summary>
        public void SetManipulatorZoom(bool enable)
        {
            _zoomEnabled = enable;
        }

        #endregion

        #region METHODS_GRAPH_VIEW

        /// <summary>
        /// Clears all graph elements and resets containers.
        /// </summary>
        public void ClearLevelView()
        {
            graphElements.ForEach(RemoveElement);
            new List<LayerContainer>(_layers.Values).ForEach(l => l.Clear());
            _defaultLayer.Clear();
            AddElement(_bound);
        }

        /// <summary>
        /// Clears expired or removed components from a specific layer.
        /// </summary>
        public void ClearLayerContainer(LBSLayer layer, bool deepClean = false)
        {
            if(layer is null) return;
            if (!_layers.TryGetValue(layer, out var container)) return;
            
            // Remove all elements
            if (deepClean)
            {
                foreach (var element in container.Clear())
                {
                    //element.style.display = DisplayStyle.None;
                    RemoveElement(element);
                }
            }
            
            // Remove expired tiles in behaviours
            foreach (var tile in layer.Behaviours.SelectMany(behaviour => behaviour.RetrieveExpiredTiles()))
            {
                //GetElementsFromLayerContainer(layer, tile).ForEach(e => e.style.display = DisplayStyle.None);
                ClearElementFromLayerContainer(tile, container);
            }
            // Remove expired tiles in assistants
            foreach (var assistant in layer.Assistants)
            {
                foreach (var tile in assistant.RetrieveExpiredTiles())
                {
                    //GetElementsFromLayerContainer(layer, tile).ForEach(e => e.style.display = DisplayStyle.None);
                    ClearElementFromLayerContainer(tile, container);
                }
            }
        }
        
        /// <summary>
        /// Clears visual representation of a single tile or component from a container.
        /// </summary>
        private void ClearElementFromLayerContainer(object tile, LayerContainer container)
        {
            if (tile == null) return;
            var gElements = container.ClearElement(tile);
            if(gElements == null) return;

            foreach (var g in gElements)
            {
                RemoveElement(g);
            }
        }
        
        /// <summary>
        /// Removes all visual elements tied to a specific component in a layer.
        /// </summary>
        public void ClearLayerComponentView(LBSLayer layer, object component)
        {
            if (!_layers.TryGetValue(layer, out var container)) return;
            if(component is null) return;
            
            var elements = container.GetElement(component);
            if(elements is null || !elements.Any()) return;
            
            foreach (var element in elements)
            {
                RemoveElement(element);
            }
        }

        /// <summary>
        /// Creates a new LayerContainer for a layer. in which all the graph elements will be stored
        /// </summary>
        public void AddContainer(LBSLayer layer)
        {
            if (_layers.ContainsKey(layer)) return;
            _layers.Add(layer, new LayerContainer());
        }

        /// <summary>
        /// Clears and removes the LayerContainer from the layer.
        /// </summary>
        public void RemoveContainer(LBSLayer layer)
        {
            ClearLayerContainer(layer, true);
            _layers.Remove(layer);
        }

        /// <summary>
        /// Saves a graphElement into a LayerContainer.
        /// </summary>
        /// <param name="layer">Layer where the graph element will be put. An LBSLayer does not hold graphElements, but it will create or get a LayerContainer object.</param>
        /// <param name="obj">The drawer from where the graphElement is created.</param>
        /// <param name="element">The graphElement to draw in screen.</param>
        public void AddElementToLayerContainer(LBSLayer layer, object obj, GraphElement element)
        {
            var container = GetLayerContainer(layer);
            if(container == null) return;
            if(element == null) return;
            element.layer = layer.index;

            container.AddElement(obj, element);
            AddElement(element);
        }

        /// <summary>
        /// Retrieves all GraphElements associated with a key in a layer container.
        /// </summary>
        public List<GraphElement> GetElementsFromLayer(LBSLayer layer, object key)
        {
            var container = GetLayerContainer(layer);

            if (container == null) return null;

            return container.GetElement(key);
        }

        /// <summary>
        /// Returns all GraphElements stored in a layer container, regardless of key.
        /// </summary>
        /// <param name="layer">The layer to retrieve elements from.</param>
        /// <returns>List of all GraphElements in the layer, or an empty list if none.</returns>
        public List<GraphElement> GetAllElementsInLayer(LBSLayer layer)
        {
            var container = GetLayerContainer(layer);
            if (container == null) return new List<GraphElement>();

            // Flatten all values in the dictionary to a single list
            return container.GetAllElements();
        }

        /// <summary>
        /// Move multiple elements in the graph
        /// </summary>
        /// <param name="Elements">Graph elements to move</param>
        /// <param name="Anchor">the top left most position of all elements</param>
        /// <param name="Offset">the offset in grid cell coordinates</param>
        public void MoveElements(List<GraphElement> Elements, Vector2Int Anchor, Vector2Int Offset)
        {
            Anchor.y *= -1;
            Offset.y *= -1;
            Anchor *= 100;
            Offset *= 100;

            Offset -= Anchor;
            foreach (var element in Elements)
            {
                Vector2 Position = element.GetPosition().position;
                Vector2Int GridPosition = new Vector2Int((int)Position.x, (int)Position.y);
                                
               // Vector2Int distanceToAnchor = GridPosition - Anchor;
                Vector2Int PostMovePosition = Offset + GridPosition;

                Rect newRectPos = new(PostMovePosition, element.GetPosition().size);

                element.SetPosition(newRectPos);
            }
}

        /// <summary>
        /// Retrieves an existing container for a layer.
        /// </summary>
        private LayerContainer GetLayerContainer(LBSLayer layer)
        {
            if (layer == null) return null;

            if (!_layers.TryGetValue(layer, out var container))
            {
                container = new LayerContainer();
                _layers.Add(layer, container);
            }
            return container;
        }

        internal void ClearLayer(LBSLayer layer)
        {
            if (_layers.ContainsKey(layer))
            {
                foreach(var element in _layers[layer].Delete())
                {
                    RemoveElement(element);
                }

                _layers.Remove(layer);
            }
        }

        #endregion
    }
}