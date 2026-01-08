using ISILab.Commons.Utility.Editor;
using ISILab.LBS.Components;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using ISILab.LBS.Plugin.Components.Behaviours;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.VisualElements
{
    [UxmlElement]
    public partial class AddonConnectionView : LBSCustomEditor
    {
        #region FIELDS
        [SerializeField]
        Addon_Unlock unlock;
        private static VisualTreeAsset visualTree { get; set; }
        #endregion

        #region VIEW FIELDS
        LBSCustomImage connectionIcon;
        LBSCustomLabelIcon layerDisplay;
        #endregion

        #region Constructors
        public AddonConnectionView()
        {
            CreateVisualElement();
        }

        public AddonConnectionView(Addon_Unlock unlock)
        {
            SetInfo(unlock);
            CreateVisualElement();
           
        }
        #endregion

        #region METHODS

        public override void SetInfo(object paramTarget)
        {
            this.unlock = (Addon_Unlock)paramTarget;    
        }

        protected override VisualElement CreateVisualElement()
        {
            if (visualTree is null)
            {
                visualTree = DirectoryTools.GetAssetByName<VisualTreeAsset>("AddonConnectionView", true);
            }

            visualTree.CloneTree(this);

            connectionIcon = this.Q<LBSCustomImage>("ConnectionIcon");
            layerDisplay = this.Q<LBSCustomLabelIcon>("LayerDisplay");

            if (unlock is not null)
            {
                unlock.OnConnectionChange += OnChange;


            }

            return this;

        }

        private void OnChange(DirConnection connection)
        {
            connection.connections.ForEach(conn =>
            {
                connectionIcon.LBSImage = SchemaTileConnectionView.GetIcon(conn.connection);
            });
            layerDisplay.Label = connection.layer.Name;
        }

        #endregion
    }
}