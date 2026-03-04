using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlObject]
    public partial class LBSTreeData
    {
        //Automatic create an object global unique id
        private Guid uniqueID;
        public LBSTreeData()
        {
            uniqueID = Guid.NewGuid();
            //Debug.Log(uniqueID.ToString());
            //Debug.Log(uniqueID.GetHashCode());
            ItemName = "Sample Tree Item";
            Id = uniqueID.GetHashCode();
        }
        
        public LBSTreeData(int _id): this(){
            Id = _id;
            ItemName = this.ToString();
        }
        
        [UxmlAttribute("id")] public int Id;
        
        [UxmlAttribute ("item-name")] public string ItemName;
        
        [UxmlObjectReference]
        public List<LBSTreeData> Children;

        public virtual TreeViewItemData<string> AsTreeDataString()
        {
            List<TreeViewItemData<string>> childData  = new();
            if (Children == null)
            {
                return new TreeViewItemData<string>(Id, ItemName);
            } else {
                
                foreach (LBSTreeData child in Children)
                {
                    childData.Add(child.AsTreeDataString()); // Recursive hell
                }
                return new TreeViewItemData<string>(Id, ItemName, childData);
            }
        }

        public virtual TreeViewItemData<LBSTreeData> AsTreeDataRaw()
        {
            List<TreeViewItemData<LBSTreeData>> childData = new();
            if (Children == null)
            {
                return new TreeViewItemData<LBSTreeData>(Id, this);
            } else {
                
                foreach (LBSTreeData child in Children)
                {
                    childData.Add(new TreeViewItemData<LBSTreeData>(child.Id, child)); // Recursive hell
                }
                return new TreeViewItemData<LBSTreeData>(Id, this, childData);
            }
        }
        
        public override string ToString()
        {
            return $"{ItemName}::{Id}";
        }

        public LBSTreeData FromString(string _rawValue)
        {
            LBSTreeData data = new LBSTreeData();
            string[] rawSplit = _rawValue.Split("::");
            data.Id = int.Parse(rawSplit[0]);
            data.ItemName = rawSplit[1];
            
            return data;
        }
    }
