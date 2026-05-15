using ISILab.DevTools.Macros;
using ISILab.LBS.Macros;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.MapTools.Generators;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ISILab.AI.Grammar
{


    /// <summary>
    /// The action that gest added into the graph for example:
    ///     go to
    ///     explore
    ///     kill
    /// </summary>
    [Serializable]
    public class GrammarTerminal : GrammarElement
    {
        private const string defaultIconGuid = "bb0770b945366c94c822cf3255eb885d";
        private static readonly Color fallbackColor = Color.white;

        // polymorphic classes use ref
        [SerializeReference]
        public List<GrammarField> fields = new();

        // meant to be used by the code generator, not serialized
        [HideInInspector]
        public string generatedClassName = string.Empty;

        // the monobehvior that gets instanced and added into a gameobject in the scene
        [SerializeField]
        private UnityEditor.MonoScript script;

        public UnityEditor.MonoScript Script
        {
            get
            {
                if (script == null && generatedClassName != string.Empty)
                {
                    var scriptGuid = AssetDatabase.FindAssets($"{generatedClassName} t:MonoScript");
                    if(scriptGuid != null && scriptGuid.Length != 0)
                    {
                        Script = LBSAssetMacro.LoadAssetByGuid<MonoScript>(scriptGuid[0]);
                    }
                }
                return script;
            }

            set
            {
                if (value == null)
                    return;
                script = value;

                // assign reference to script - its readonly. Terminal in a questtrigger can only be assigned here
                var scriptGuid = AssetMacro.GetGuidFromAsset(script);
                generatedClassName = script.name;
            }
        }

        public override void OnEnable()
        {
            // Safe: happens after Unity initialization
            var settings = LBSSettings.Instance;

            if(color == null)
            {
                color = settings != null
                ? settings.view.behavioursColor
                : fallbackColor;
            }
      
            iconGuid = defaultIconGuid;
            // call the getter just to read the generated class name
            var script = Script;
            base.OnEnable();
        }
    }
}