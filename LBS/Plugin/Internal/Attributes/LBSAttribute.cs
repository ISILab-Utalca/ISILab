using System;
using UnityEngine;

namespace ISILab.LBS
{
    public abstract class LBSAttribute : Attribute { }

    [Obsolete("This attribute is not in use. Deletion will be decided after observing LBSCharacteristicAttribute deletion effects.")]
    [AttributeUsage(AttributeTargets.Class)]
    public class LBSSearchAttribute : LBSAttribute
    {
        private string name;
        private string iconPath;
    
        public string Name => name;
        public Texture2D Icon => null; // TODO: Implement default icon
    
        public LBSSearchAttribute(string name, string iconPath)
        {
            this.name = name;
            this.iconPath = iconPath;
        }
    }

    //[System.AttributeUsage(System.AttributeTargets.Class)]
    //public class LBSCharacteristicAttribute : LBSSearchAttribute
    //{
    //    public LBSCharacteristicAttribute(string name, string iconPath) : base(name, iconPath) { }
    //}
}