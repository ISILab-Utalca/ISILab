using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace ISILab.LBS.CustomComponents
{
    
    [UxmlElement]
    public partial class LBSCustomBreadcrumbs : ToolbarBreadcrumbs
    {

        string[] m_titles;

        [UxmlAttribute]
        public string[] SectionTitles
        {
            get => m_titles;
            set
            {
                m_titles = value;
                DrawBreadcrumbsItems();
            }
        }
        
        public LBSCustomBreadcrumbs() : base()
        {
            this.AddToClassList("lbs-breadcrumbs");
        }

        public LBSCustomBreadcrumbs(List<string> _labelsToDisplay): base()
        {
            m_titles = _labelsToDisplay.ToArray();
            DrawBreadcrumbsItems();
        }

        public void DrawBreadcrumbsItems()
        {
            if (this.childCount > 0) this.Clear();
            if (SectionTitles == null) return;
            
            foreach (var title in SectionTitles)
            {
                this.PushItem(title, null);
            }
            
        }
    }
}


