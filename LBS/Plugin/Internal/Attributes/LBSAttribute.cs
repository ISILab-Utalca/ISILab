using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ISILab.LBS
{
    public abstract class LBSAttribute : Attribute { }

    [Obsolete("This attribute is not in use. Deletion will be decided after observing LBSCharacteristicAttribute deletion effects.")]
    [AttributeUsage(AttributeTargets.Class)]
    public class LBSSearchAttribute : LBSAttribute
    {
        private string name;
        private string iconPath;
    
        public string Name => name;
        public Texture2D Icon => null; // TODO: Implement default icon
    
        public LBSSearchAttribute(string name, string iconPath)
        {
            this.name = name;
            this.iconPath = iconPath;
        }
    }

    /// <summary>
    /// Atributo que marca un campo o propiedad para que sea mostrado en el "Template Inspector" del editor.
    /// </summary>
    /// <remarks>
    /// Este atributo se aplica sobre campos o propiedades (ver __AttributeUsage__). Su propósito es permitir
    /// al código del editor filtrar y mostrar únicamente los miembros relevantes cuando se edita una plantilla.
    /// No cambia comportamiento en tiempo de ejecución y está pensado para uso exclusivo en el entorno de edición.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ShowOnLayerTemplateAttribute : LBSAttribute
    {
        /// <summary>
        /// Constructor por defecto. Aplicar el atributo a un miembro indica que debe aparecer en el Template Inspector.
        /// </summary>
        public ShowOnLayerTemplateAttribute() { }

        /// <summary>
        /// Obtiene los miembros de instancia del objeto especificado que están anotados con <see cref="ShowOnLayerTemplateAttribute"/>.
        /// </summary>
        /// <param name="obj">Instancia cuyo tipo se inspecciona.</param>
        /// <returns>
        /// Array de <see cref="FieldInfo"/> que representa los campos marcados con el atributo.
        /// Retorna un array vacío si no se encuentran coincidencias, o si obj es mull.
        /// </returns>
        /// <remarks>
        /// La búsqueda incluye campos y propiedades, públicos y no públicos de instancia.
        /// </remarks>
        public static MemberInfo[] GetMembers(object obj)
        {
            List<MemberInfo> memberInfos = new List<MemberInfo>();
            if (obj is null) return memberInfos.ToArray();

            var members = obj.GetType().GetMembers(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

            foreach(var member in members)
            {
                var attribute = member.GetCustomAttribute<ShowOnLayerTemplateAttribute>();

                if (attribute == null)
                    continue;
                memberInfos.Add(member);
            }
            return (memberInfos.ToArray());
        }
    }

        //[System.AttributeUsage(System.AttributeTargets.Class)]
        //public class LBSCharacteristicAttribute : LBSSearchAttribute
        //{
        //    public LBSCharacteristicAttribute(string name, string iconPath) : base(name, iconPath) { }
        //}
}