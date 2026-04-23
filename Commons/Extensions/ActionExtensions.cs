using System;
using System.Linq;

namespace ISILab.Extensions
{
    public static class ActionExtensions
    {
        /// <summary>
        /// Adds a function from an action only if it does not exist in the invocation list.
        /// </summary>
        public static void AddUnique<T>(ref Action<T> source, Action<T> handler)
        {
            if (handler == null) return;
            if (source == null || !source.GetInvocationList().Contains(handler))
            {
                source = (Action<T>)Delegate.Combine(source, handler);
            }
        }

        /// <summary>
        /// Adds a function from an action only if it does not exist in the invocation list.
        /// </summary>
        public static void AddUnique(ref Action source, Action handler)
        {
            if (handler == null) return;
            if (source == null || !source.GetInvocationList().Contains(handler))
            {
                source = (Action)Delegate.Combine(source, handler);
            }
        }


        /// <summary>
        /// Removes a function from an action only if it exists in the invocation list.
        /// </summary>
        public static void RemoveUnique<T>(ref Action<T> source, Action<T> handler)
        {
            if (source == null || handler == null) return;

            if (source.GetInvocationList().Contains(handler))
            {
                source = (Action<T>)Delegate.Remove(source, handler);
            }
        }

        /// <summary>
        /// Removes a function from an action only if it exists in the invocation list.
        /// </summary>
        public static void RemoveUnique(ref Action source, Action handler)
        {
            if (source == null || handler == null) return;

            if (source.GetInvocationList().Contains(handler))
            {
                source = (Action)Delegate.Remove(source, handler);
            }
        }
    }
}