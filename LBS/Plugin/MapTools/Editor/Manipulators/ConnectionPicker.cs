using ISILab.LBS.Editor.Windows;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using LBS.Components;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Components.Behaviours;
using ISILab.LBS.VisualElements;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Characteristics;

namespace ISILab.LBS.Manipulators
{
    /// <summary>
    /// Allows selecting a population bundle from any layer and assigns it to the selected quest node if compatible.
    /// </summary>
    public class ConnectionPicker : LBSManipulator
    {
        // Private fields
        private TileGroupBehavior _behaviour;


        /// <summary>
        /// Icon used by this manipulator.
        /// </summary>
        protected override string IconGuid => "f53f51dae7956eb4b99123e868e99d67";

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

            Addon_Unlock UnlockAddon = _behaviour.SelectedTilemap.GetAddon<Addon_Unlock>();
            if (UnlockAddon is null) return;

            // The VisualElement that received the event
            VisualElement hovered = e.target as VisualElement;

            // Walk up the hierarchy if needed
            var connectionView = hovered?.GetFirstAncestorOfType<SchemaTileConnectionView>();

            if (connectionView != null)
            {
                LBSTile tile = connectionView.Tile;
                DirConnection newDirConnection = new DirConnection(tile);
                int direction = LBSDirection.ToInt(connectionView.Direction);
                string connection = connectionView.Type;

                
                newDirConnection.connections.Add((direction,connection));
                UnlockAddon.DirConnection = newDirConnection;
            }

            OnManipulationEnd?.Invoke();
        }

    }
}
