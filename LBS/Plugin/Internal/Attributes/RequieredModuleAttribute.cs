using System;
using System.Collections.Generic;
using System.Linq;
using ISILab.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Behaviours;

namespace ISILab.LBS
{
    public class RequieredModuleAttribute : Attribute
    {
        public List<Type> types;

        public RequieredModuleAttribute(params Type[] types)
        {
            this.types = types.GetDerivedTypes(typeof(LBSModule)).ToList();
        }
    }

    public class RequieredBehaviourAttribute : Attribute
    {
        public List<Type> types;

        public RequieredBehaviourAttribute(params Type[] type)
        {
            this.types = type.GetDerivedTypes(typeof(LBSBehaviour)).ToList();
        }
    }

    public class RequieredAssistantAttribute : Attribute
    {
        public List<Type> types;

        public RequieredAssistantAttribute(params Type[] type)
        {
            this.types = type.GetDerivedTypes(typeof(LBSAssistant)).ToList();
        }
    }
}