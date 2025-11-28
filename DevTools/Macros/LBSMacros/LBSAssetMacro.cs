
using System.Linq;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Internal;

namespace ISILab.LBS.Macros
{
    public static class LBSAssetMacro
    {
        
        /// <summary>
        /// Tries to return a LBSTag
        /// </summary>
        /// <param name="tag">The tag name that you are looking for</param>
        /// <returns></returns>
        public static LBSTag GetLBSTag(string tag)
        {
            var lbsTags = LBSAssetsStorage.Instance.Get<LBSTag>();
            return lbsTags.FirstOrDefault(lbsTag => lbsTag.Label == tag);
        }
    }
}


