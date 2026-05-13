using ISILab.LBS.Plugin.Components.Bundles;
using System;
using UnityEngine;


namespace ISILab.AI.Grammar
{  
    public interface IBundleFlags
    {
        // Must implmenet. ie, must add a BundleFlag field to the interfaced class
        Bundle.EElementFlag AllowedElements { get; }

        // pass n bundles or layers and determine how it's data is stored
        void SetObjectBundle(object[] objs);

        // interface functions
        bool HasAnyFlag(Bundle bundle) =>
            bundle != null && (AllowedElements & bundle.ElementFlag) != 0;

        bool HasAllFlags(Bundle bundle) =>
            bundle != null && (bundle.ElementFlag & AllowedElements) == AllowedElements;

        bool HasNoneOfFlags(Bundle bundle) =>
            bundle == null || (AllowedElements & bundle.ElementFlag) == 0;

        bool IsMissingAnyFlag(Bundle bundle) =>
            bundle == null || (bundle.ElementFlag & AllowedElements) != AllowedElements;
    }
    

    [Serializable]
    public abstract class GrammarBundleField<T> : GrammarField<T>, IBundleFlags
    {
        // by default we can assign any bundle
        [SerializeField]
        protected Bundle.EElementFlag allowedElements = (Bundle.EElementFlag)~0;

        public Bundle.EElementFlag AllowedElements => allowedElements;

        public virtual void SetObjectBundle(object[] objs)
        {
            throw new NotImplementedException();
        }
    }
}
