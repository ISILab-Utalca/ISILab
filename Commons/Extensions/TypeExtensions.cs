using System;
using System.Collections.Generic;
using System.Linq;

namespace ISILab.Extensions
{
    /// <summary>
    /// Class with extension methods for Type.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets from a collection all types that are derived from a specified base type.
        /// </summary>
        /// <param name="types">The collection to inspect.</param>
        /// <param name="baseType">The base type from which the derived types are consulted.</param>
        /// <returns>All types from the collection that are derived from <paramref name="baseType"/></returns>
        public static IEnumerable<Type> GetDerivedTypes(this IEnumerable<Type> types, Type baseType)
        {
            return types.Where(t => baseType.IsAssignableFrom(t) && t != baseType);
        }

        /// <summary>
        /// Gets all types in the Current Domain that are derived from a specified base type.
        /// </summary>
        /// <param name="baseType">The base type from which the derived types are consulted.</param>
        /// <returns>All types that are derived from <paramref name="baseType"/></returns>
        public static IEnumerable<Type> GetDerivedTypes(this Type baseType)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => baseType.IsAssignableFrom(type) && type != baseType);
        }
    }
}

