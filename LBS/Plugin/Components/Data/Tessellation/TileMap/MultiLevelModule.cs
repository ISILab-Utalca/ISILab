using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Modules
{
    /*[Serializable]
    public class MultiLevelModule : LBSModule//, ISelectable
    {
        public int CurrentLevel { get { return currentLevel; } }
        private int currentLevel = 0;
        public int MaxLevel { get { return maxLevel; } }
        private int maxLevel = 2;

        [SerializeField]
        private List<LBSModule>[] modulesByLevel = new List<LBSModule>[10];

        public MultiLevelModule() { }


        // Initialization
        public void SetModulesByLevel(int index, List<LBSModule> modules)
        {
            var modulesCopy = modules.Clone();
            modulesCopy.RemoveAll(m => m.GetType() == typeof(MultiLevelModule));
            modulesByLevel[index] = modulesCopy;
        }

        public void FillEmptyLevels(List<LBSModule> templateModules)
        {
            for (int i = 0; i <= maxLevel; i++)
            {
                if (modulesByLevel[i] is null)
                {
                    var modClone = templateModules.Clone();
                    modClone.RemoveAll(m => m.GetType() == typeof(MultiLevelModule));
                    modulesByLevel[i] = modClone;
                }
            }
            //PrintModuleMatrix();
        }


        // Actions
        public void ChangeLevel(uint currentLevelIndex, uint nextLevelIndex)
        {
            var activeModules = OwnerLayer.Modules(currentLevel);
            activeModules.RemoveAll(m => m.GetType() == typeof(MultiLevelModule));
            
            var newModules = modulesByLevel[nextLevelIndex].Clone();

            for (int i = 0; i < activeModules.Count; i++)
            {
                // Retrieving corresponding module in nextlevel module list
                var mod = newModules.Find(m => m.GetType() == activeModules[i].GetType());
                newModules.Remove(mod);

                // Copying old value in currentlevel module list before swapping
                if(i < modulesByLevel[currentLevelIndex].Count)
                {
                    modulesByLevel[currentLevelIndex][i] = activeModules[i];
                }


                OwnerLayer.ReplaceModule(activeModules[i], mod);
            }
            currentLevel = (int) nextLevelIndex;
            //PrintModuleMatrix();
        }

        public List<LBSModule> GetModulesByLevel(int index)
        {
            return modulesByLevel[index];
        }
            


        // Must have
        public List<object> GetSelected(Vector2Int position)
        {
            return null;
        }

        public override void Clear()
        {
        }

        public override object Clone()
        {
            var newModule = new MultiLevelModule();
            for (int i = 0; i < modulesByLevel.Length; i++)
            {
                if(modulesByLevel[i] is null)
                {
                    newModule.modulesByLevel[i] = null;
                    continue;
                }
                newModule.modulesByLevel[i] = new (modulesByLevel[i]);
            }
            return newModule;
        }

        public override bool IsEmpty()
        {
            return true;
        }


        // Utils
        public void PrintModuleMatrix()
        {

            string s = "";
            for (int i = 0; i < modulesByLevel.Length; i++)
            {
                if (modulesByLevel[i] is null)
                {
                    s += "null\n";
                    continue;
                }

                foreach (var module in modulesByLevel[i])
                {
                    if (module is null)
                    {
                        s += "null - ";
                    }
                    else
                    {
                        s += $"{module.GetType().ToString()} - ";
                    }
                }
                s += "\n";
            }
            Debug.Log(s);
        }
    }//*/

}
