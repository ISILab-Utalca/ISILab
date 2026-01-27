using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge;
using UnityEngine;
using UnityEngine.UIElements;

//GABO TODO: TERMINAR
namespace ISILab.LBS.VisualElements
{
    public class SimulationObstacleView : VisualElement
    {

        #region FIELDS
        //PathOSTile triggerTile;
        #endregion

        #region FIELDS VIEW
        public VisualElement icon;
        public Label positionLabel;
        public Label stateLabel;
        public Button removeButton;
        #endregion

        #region EVENTS
        Action OnRemove;
        #endregion

        #region PROPERTIES
        public Texture2D Icon
        {
            set => icon.style.backgroundImage = value;
        }
        #endregion

        #region CONSTRUCTORS
        public SimulationObstacleView(SimulationTile triggerTile, SimulationTile obstacleTile, LBSSimulationObstacleConnections.Category category)
        {
            var visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("PathOSObstacleView");
            visualTree.CloneTree(this);

            this.icon = this.Q<VisualElement>("Icon");
            this.positionLabel = this.Q<Label>("PositionLabel");
            this.stateLabel = this.Q<Label>("StateLabel");
            this.removeButton = this.Q<Button>("RemoveButton");

            // Set fields
            SetFields(triggerTile, obstacleTile, category);
        }
        #endregion

        #region METHODS
        private void SetFields(SimulationTile triggerTile, SimulationTile obstacleTile, LBSSimulationObstacleConnections.Category category)
        {
            // Obstacle object+trigger tile check
            if (!obstacleTile.IsDynamicObstacleObject) { Debug.LogWarning("PathOSObstacleView.SetFields(): Tile dada no es obstaculo!"); return; }

            icon.style.backgroundImage = new StyleBackground(obstacleTile.Tag.Icon);
            //positionLabel.text = $"{obstacleTile.X} x {obstacleTile.Y}";
            stateLabel.text = category.ToString();
            // Suscripciones a boton
            OnRemove -= () => triggerTile.RemoveObstacle(obstacleTile);
            OnRemove += () => triggerTile.RemoveObstacle(obstacleTile);
            removeButton.clicked -= OnRemove;
            removeButton.clicked += OnRemove;
        }
        #endregion
    }
}