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
        private int m_selectSecction = -1;

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

        [UxmlAttribute]
        public int SelectSection
        {
            get => m_selectSecction;
            set
            {
                m_selectSecction = value;
                SelectAtIndex(m_selectSecction);
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
        
        public void SelectAtIndex(int _index)
        {
            if (childCount == 0) return;
            if (_index < 0 || _index >= this.childCount)
            {
                UnselectAll();
                return;
            }
            
            for (int i = 0; i < this.childCount; i++)
            {
                VisualElement breadcrumsElement  = this.ElementAt(i);
                if  (breadcrumsElement == null) continue;
                if (i == _index)
                {
                    breadcrumsElement.AddToClassList("lbs-breadcrumb-item-selected");
                }
                else
                {
                    breadcrumsElement.RemoveFromClassList("lbs-breadcrumb-item-selected");
                }
            }
        }

        private void UnselectAll()
        {
            for (int i = 0; i < this.childCount; i++)
            {
                VisualElement breadcrumsElement = this.ElementAt(i);
                if (breadcrumsElement == null) continue;
                breadcrumsElement.RemoveFromClassList("lbs-breadcrumb-item-selected");
            }
        }
    }
}


