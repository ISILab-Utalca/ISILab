using LBS.Bundles;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ISILab.LBS.Plugin.Components.Bundles;
using UnityEngine;

namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Requiere que las cosas que hereden de el tengan un contructor por defecto sin parametros
    /// </summary>
    [System.Serializable]
    public abstract class LBSCharacteristic : ICloneable
    {
        #region FIELDS
        public static readonly bool unique = true;

        public static List<List<Type>> exclusives = new List<List<Type>>()
        {
            new List<Type>(){typeof(LBSMainInteriorBundle), typeof(LBSMainExteriorBundle), typeof(LBSMainPopulationBundle)}
        };

        [SerializeReference, SerializeField]
        private Bundle owner;

        protected bool initialized = false;
        #endregion

        #region PROPERTIES
        [JsonIgnore, HideInInspector]
        public Bundle Owner
        {
            get => owner; 
            set => owner = value;
        }
        #endregion

        #region CONSTRUCTORS
        [SerializeField]
        public LBSCharacteristic() {   }
        #endregion

        #region METHODS
        /// <summary>
        /// this function allow the characteristic known what bundle its is owner
        /// asi podemos tener acciones o itenracciones dentro characteristics
        /// </summary>
        public void Init(Bundle owner)
        {
            if (initialized) return;

            this.owner = owner;
            OnEnable();
            initialized = true;
        }

        public static bool IsUnique(Type t)
        {
            var field = t.GetField(nameof(unique),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            return (bool)(field?.GetValue(null) ?? false);
        }

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

        public virtual void OnEnable() {  }

        public virtual void OnRefresh() { }

        public abstract object Clone();


        public abstract override bool Equals(object obj);

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public virtual List<string> Validate()
        {
            return  new List<string>();
        }
        #endregion

    }
}