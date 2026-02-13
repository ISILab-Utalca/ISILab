using ISILab.LBS.Macros;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ISILab.LBS.Components
{
    [Serializable]
    public abstract class BlueprintStorable
    {
        public BlueprintStorable(object[] objs)
        {
            if (objs == null || !objs.Any()) return;
            SetBlueprintData(objs);
        }

        public abstract object GetBlueprintData();
        public abstract void SetBlueprintData(object[] objs);
    }

}
