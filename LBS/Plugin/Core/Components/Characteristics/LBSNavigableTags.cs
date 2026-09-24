using ISILab.Commons.Extensions;
using ISILab.LBS.Components;
using ISILab.LBS.Plugin.Internal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Characteristic used to mark which connection tags from the children bundles' tiles are meant to be traversable by a in-game character.<br/>
    /// This info is useful for Population's MAP Elites algorithm and Simulation's agent.
    /// </summary>
    [System.Serializable]
    //[LBSCharacteristic("Navigable Tags", "")]
    public class LBSNavigableTags : LBSCharacteristic
    {
        [SerializeField]
        private List<LBSTag> tags = new List<LBSTag>();

        [SerializeField]
        private List<bool> navigable = new List<bool>();

        private Dictionary<LBSTag, bool> navigableTags = new Dictionary<LBSTag, bool>();


        /// <summary>
        /// Pool of tags contained by children bundles.
        /// </summary>
        public List<LBSTag> Tags => new List<LBSTag>(tags);
        /// <summary>
        /// List of traversability values of the tags.
        /// </summary>
        public List<bool> Navigable => navigable;
        /// <summary>
        /// Pairing of tags and its traversability values.
        /// </summary>
        public Dictionary<LBSTag, bool> NavigableTagsRef => navigableTags;

        /// <summary>
        /// Read every tag in children bundles and lists them.
        /// </summary>
        public void SetTags()
        {
            List<LBSTag> identifierTags = LBSAssetsStorage.Instance.Get<LBSTag>();
            List<LBSDirection> connections = Owner.GetChildrenCharacteristics<LBSDirection>();
            List<string> tags = connections.SelectMany(c => c.Connections).ToList().RemoveDuplicates();
            List<LBSTag> idents = tags.Select(s => identifierTags.Find(i => s == i.Label)).ToList().RemoveEmpties();

            bool tagsChanged = !this.tags.SequenceEqual(idents);

            this.tags = new List<LBSTag>(idents);
            if (tagsChanged)
            {
                Debug.Log("Tags Changed");
                navigable.Clear();
                for (int i = 0; i < idents.Count; i++)
                    navigable.Add(false);
            }
            navigableTags.Clear();

            for(int i = 0; i < idents.Count; i++)
            {
                LBSTag tag = idents[i];
                if (tag == null) continue;
                navigableTags.Add(tag, navigable[i]);
                //if(!navigableTags.ContainsKey(tag))
                //{
                //    navigableTags.Add(tag, false);
                //};
            }
        }

        /// <summary>
        /// Gets the labels of tags that are marked as navigable.
        /// </summary>
        /// <returns>A list of all navigable tags' labels.</returns>
        public List<string> GetNavigableTags()
        {
            return navigableTags.Keys.Where(t => navigableTags[t]).Select(t => t.Label).ToList();
        }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (obj is not LBSNavigableTags other) return false;

            return other.NavigableTagsRef.Equals(NavigableTagsRef);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override List<string> Validate()
        {
            var w = new List<string>();
            if (!navigable.Any())
                w.Add("It is recommended to mark at least one tag as navigable in order to ensure MAP Elites and Simulation compatibility.");
            return w;
        }
    }
}
