using ISILab.LBS.Plugin.Components.Bundles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Base class for <see cref="Bundle"/> characteristics. A LBSCharacteristic provides information and functionality to bundles, allowing customized behavior.
    /// </summary>
    [System.Serializable]
    public abstract class LBSCharacteristic : ICloneable
    {
        #region FIELDS
        /// <summary>
        /// Prevents this characteristic from being duplicated in the same <see cref="Bundle"/>. Overwrite using 'new' keyword if usage of multiple instances of the same characteristic is intended.
        /// </summary>
        public static readonly bool unique = true;

        /// <summary>
        /// Defines groups of characteristics that should not coexist in the same <see cref="Bundle"/>.
        /// </summary>
        public static List<List<Type>> exclusives = new List<List<Type>>()
        {
            new List<Type>(){typeof(LBSMainInteriorBundle), typeof(LBSMainExteriorBundle), typeof(LBSMainPopulationBundle)}
        };

        [SerializeReference, SerializeField]
        private Bundle owner;

        /// <summary>
        /// Flag indicating whether this characteristic needs to be initialized or not.
        /// </summary>
        protected bool initialized = false;
        #endregion

        #region PROPERTIES
        /// <summary>
        /// A reference to its corresponding <see cref="Bundle"/>.
        /// </summary>
        [JsonIgnore, HideInInspector]
        public Bundle Owner
        {
            get => owner; 
            set => owner = value;
        }
        #endregion

        #region CONSTRUCTORS
        public LBSCharacteristic() {   }
        #endregion

        #region METHODS
        /// <summary>
        /// If not already initialized, this method performs all needed initialization operations.<br />
        /// To extend this, <see cref="OnEnable"/> should be overridden.
        /// </summary>
        /// <param name="owner">Owner <see cref="Bundle"/> of this characteristic.</param>
        public void Init(Bundle owner)
        {
            if (initialized) return;

            this.owner = owner;
            OnEnable();
            initialized = true;
        }

        /// <summary>
        /// Indicates whether a <see cref="LBSCharacteristic"/> is marked as unique.
        /// </summary>
        /// <param name="t">The type of the <see cref="LBSCharacteristic"/> to consult.</param>
        /// <returns>True if the consulted <see cref="LBSCharacteristic"/> is marked as unique. False otherwise.</returns>
        public static bool IsUnique(Type t)
        {
            var field = t.GetField(nameof(unique),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            return (bool)(field?.GetValue(null) ?? false);
        }

        /// <summary>
        /// Indicates whether a <see cref="LBSCharacteristic"/> is exclusive with other existent characteristics.
        /// </summary>
        /// <param name="t">The type of the <see cref="LBSCharacteristic"/> to consult.</param>
        /// <param name="exclusivenessGroups">All known groups of exclusive <see cref="LBSCharacteristic"/>.</param>
        /// <returns>True if the consulted <see cref="LBSCharacteristic"/> is listed as exclusive with any other characteristic. False otherwise.</returns>
        public static bool IsExclusive(Type t, out List<List<Type>> exclusivenessGroups)
        {
            bool isExclusive = false;
            exclusivenessGroups = new List<List<Type>>();
            foreach(List<Type> group in exclusives)
            {
                if (group.Contains(t))
                {
                    exclusivenessGroups.Add(group);
                    isExclusive = true;
                }
            }
            return isExclusive;
        }

        /// <summary>
        /// Called only when this characteristic is initialized.
        /// </summary>
        public virtual void OnEnable() { }

        /// <summary>
        /// Called when inspector refresh is requested.
        /// </summary>
        public virtual void OnRefresh() { }

        /// <summary>
        /// <see cref="ICloneable"/> implementation.
        /// </summary>
        /// <returns></returns>
        public abstract object Clone();


        public abstract override bool Equals(object obj);

        

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Checks for any anomaly in the configuration of this characteristic.
        /// </summary>
        /// <returns>A list of warnings indicating what needs to be fixed.</returns>
        public virtual List<string> Validate()
        {
            return new List<string>();
        }
        #endregion

    }
}