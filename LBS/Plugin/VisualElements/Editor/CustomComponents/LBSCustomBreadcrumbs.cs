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

        string[] mTitles;
        private int mSelectSection = -1;
        
        public static event Action OnBreadcrumbClickEvent;

        [UxmlAttribute]
        public string[] SectionTitles
        {
            get => mTitles;
            set
            {
                mTitles = value;
                DrawBreadcrumbsItems();
            }
        }

        [UxmlAttribute]
        public int SelectSection
        {
            get => mSelectSection;
            set
            {
                mSelectSection = value;
                SelectAtIndex(mSelectSection);
            }
        }

        public LBSCustomBreadcrumbs()
        {
            this.AddToClassList("lbs-breadcrumbs");
        }

        public LBSCustomBreadcrumbs(List<string> _labelsToDisplay): this()
        {
            mTitles = _labelsToDisplay.ToArray();
            DrawBreadcrumbsItems();
        }

        public void DrawBreadcrumbsItems()
        {
            if (this.childCount > 0) this.Clear();
            if (SectionTitles == null) return;
            
            for (int i = 0; i < SectionTitles.Length; i++)
            {
                string title = SectionTitles[i];
                this.PushItem(title, OnBreadcrumbClickEvent);
            }
        }
        
        
        private void SelectAtIndex(int _index)
        {
            if (childCount == 0) return;
            if (_index < 0 || _index >= this.childCount)
            {
                UnselectAll();
                return;
            }
            
            for (int i = 0; i < this.childCount; i++)
            {
                VisualElement breadcrumbsElement  = this.ElementAt(i);
                if  (breadcrumbsElement == null) continue;
                if (i == _index)
                {
                    breadcrumbsElement.AddToClassList("lbs-breadcrumb-item-selected");
                }
                else
                {
                    breadcrumbsElement.RemoveFromClassList("lbs-breadcrumb-item-selected");
                }
            }
        }

        private void UnselectAll()
        {
            for (int i = 0; i < this.childCount; i++)
            {
                VisualElement breadcrumbElement = this.ElementAt(i);
                if (breadcrumbElement == null) continue;
                breadcrumbElement.RemoveFromClassList("lbs-breadcrumb-item-selected");
            }
        }
    }
}


