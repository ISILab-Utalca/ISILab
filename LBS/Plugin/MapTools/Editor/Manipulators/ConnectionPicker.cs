using ISILab.LBS.Behaviours;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.VisualElements;
using LBS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    /// <summary>
    /// Allows selecting a population bundle from any layer and assigns it to the selected quest node if compatible.
    /// </summary>
    public class ConnectionPicker : LBSManipulator
    {
        // picker button that was used to activate the manipulator
        private PickerConnect activator;

        // Private fields
        private TileGroupBehavior _behaviour;

        public Action<SchemaTileConnectionView, ConnectionData> OnConnectionClicked;


        public PickerConnect Activator { get; set; }
        /// <summary>
        /// Icon used by this manipulator.
        /// </summary>
        protected override string IconGuid => "810162c8614a2a544bcce7bbe955a81b";

        public ConnectionPicker()
        {
            Name = "Pick an interior connection";
            Description = "Binds an interior connection to population tile with the" +
                "(Trigger) or (Key), to be opened";
        }

        public override void Init(LBSLayer layer, object owner = null)
        {
            base.Init(layer, owner);
            _behaviour = layer.GetBehaviour<TileGroupBehavior>();
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            if (_behaviour is null) return;
            if (_behaviour.SelectedTilemap is null) return;

            //Addon_Unlock UnlockAddon = _behaviour.SelectedTilemap.GetAddon<Addon_Unlock>();
            //if (UnlockAddon is null) return;

            // The VisualElement that received the event
            VisualElement hovered = e.target as VisualElement;

            // Walk up the hierarchy if needed
            var connectionView = hovered?.GetFirstAncestorOfType<SchemaTileConnectionView>();

            if (connectionView != null)
            {
                LBSTile tile = connectionView.Tile;
                ConnectionData connData = new ConnectionData(connectionView.Layer, tile);
                int direction = LBSDirection.ToInt(connectionView.Direction);
                string connection = connectionView.Type;

                
                connData.connections.Add(new DirConnection(direction,connection));
                OnConnectionClicked.Invoke(connectionView, connData);
            }

            OnManipulationEnd?.Invoke();
        }

    }
}
