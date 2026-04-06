
using System;
using System.Linq;

namespace ISILab.Extensions 
{
    public static class ActionExtensions
    {
        /// <summary>
        /// Adds a function to an action only if its not part of the invocation list
        /// </summary>
        /// <typeparam name="T">no need to specify as long as both action and function share params</typeparam>
        /// <param name="source">Action field</param>
        /// <param name="handler">Function that will be invoked</param>
        public static void AddUnique<T>(
            ref Action<T> source,
            Action<T> handler)
        {
            if (source == null ||
                !source.GetInvocationList().Contains(handler))
            {
                source = (Action<T>)Delegate.Combine(source, handler);
            }
        }
    }
}