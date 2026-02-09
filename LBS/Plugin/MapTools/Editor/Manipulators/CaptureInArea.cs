using ISILab.Commons;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Editor.Windows;
using LBS.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Manipulators
{
    public class CaptureInArea : ManipulateTeselation
    {

        public object[] capturedObjects;

        protected override string IconGuid { get => "089a07d25e2a0a347b3e1ad8e0c2818b"; }

        public CaptureInArea():base(){}

        public override void Init(LBSLayer layer, object owner)
        {

          //  base.Init(layer, owner);
        }

        protected override void OnMouseUp(VisualElement element, Vector2Int endPosition, MouseUpEvent e)
        {
            List<object> areaObjs = new();

            // Should get all layers under the start and endposition 
            foreach (LBSLayer layer in LBSMainWindow.Instance.GetLayers())
            {
                areaObjs.AddRange(layer.GetObjects(StartPosition, EndPosition));
            }

            object[] objs = areaObjs.ToArray();
            Debug.Log($"Captured {objs.Length} objects.");
            // Use the objects to save them and create a blueprint
        }
    }
}