using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ISILab.LBS.Characteristics;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using LBS.Components.TileMap;
using UnityEngine;

namespace ISILab.Extensions
{
    public static class LBSCharacteristicExtensions
    {
        public static LBSTag FirstTag(this LBSCharacteristic characteristic)
        {
            if (characteristic is not LBSTagsCharacteristic tagChar) return null;

            return tagChar[0];
        }
    }
}