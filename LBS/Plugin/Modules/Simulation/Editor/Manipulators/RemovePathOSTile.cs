using ISILab.LBS.Manipulators;
using ISILab.LBS.VisualElements;
using LBS.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ISILab.LBS.Behaviours;

namespace ISILab.LBS.Plugin.Modules.Simulation.Editor.Manipulators
{
    public class RemovePathOSTile : LBSManipulator
    {
        #region FIELDS
        SimulationBehaviour behaviour;

        #endregion

        #region PROPERTIES
        protected override string IconGuid => "ce08b36a396edbf4394f7a4e641f253d";
        #endregion

        #region CONSTRUCTORS
        public RemovePathOSTile() : base()
        {
            Feedback = new AreaFeedback();
            Feedback.fixToTeselation = true;
        }
        #endregion

        #region METHODS
        public override void Init(LBSLayer layer, object provider)
        {
            base.Init(layer, provider);

            behaviour = provider as SimulationBehaviour;
            Feedback.TeselationSize = layer.TileSize;
            layer.OnTileSizeChange += (val) => Feedback.TeselationSize = val;
        }

        protected override void OnMouseUp(VisualElement target, Vector2Int endPosition, MouseUpEvent e)
        {
            //GABO TODO: ARREGLAR LOGICA UNDO
            // Inicio logica UNDO
            var x = LBSController.CurrentLevel;
            EditorGUI.BeginChangeCheck();
            Undo.RegisterCompleteObjectUndo(x, "Remove PathOS Tile");

            // Remover PathOSTiles mediante PathOSBehaviour
            var corners = behaviour.OwnerLayer.ToFixedPosition(StartPosition, EndPosition);
            for (int i = corners.Item1.x; i <= corners.Item2.x; i++)
            {
                for (int j = corners.Item1.y; j <= corners.Item2.y; j++)
                {
                    behaviour.RemoveTile(i, j);
                }
            }

            // Final logica UNDO
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(x);
            }
        }
        #endregion
    }
}
