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

        /// <summary>
        /// Scours the invocation list and completely strips any delegate matching a specific method name,
        /// regardless of which object instance it targets.
        /// </summary>
        public static void RemoveMethod<T>(ref Action<T> del, string methodName)
        {
            if (del == null) return;

            var invocationList = del.GetInvocationList();
            foreach (var existingDel in invocationList)
            {
                // Check if the underlying method name matches your target function
                if (existingDel.Method.Name == methodName)
                {
                    del -= (Action<T>)existingDel;
                }
            }
        }

        /// <summary>
        /// Cleans out any delegates whose target instance is an old or orphaned instance of a specific view type.
        /// </summary>
        public static void RemoveAllByTargetType<TDelegate, TTargetType>(ref Action<TDelegate> del)
        {
            if (del == null) return;

            // 1. Get all delegates currently registered
            var invocationList = del.GetInvocationList();

            // 2. Filter out ANY delegate whose target object matches our blacklisted type
            var cleanList = invocationList
                .Where(d => d.Target == null || !(d.Target is TTargetType))
                .Select(d => (Action<TDelegate>)d)
                .ToArray();

            // 3. Clear the original delegate ref completely
            del = null;

            // 4. Combine the clean items back into a fresh chain
            if (cleanList.Length > 0)
            {
                del = (Action<TDelegate>)Delegate.Combine(cleanList);
            }
        }

        /// <summary>
        /// Purges any delegate that belongs to a QuestNodeView unless it matches the active view instance.
        /// </summary>
        public static void RemoveVisualElementMethods<TDelegate>(ref Action<TDelegate> del, object currentValidView)
        {
            if (del == null) return;

            var invocationList = del.GetInvocationList();

            // Filter out ANY delegate belonging to a view that isn't our fresh current instance
            var cleanList = invocationList
                .Where(d =>
                    d.Target == null ||
                    !(d.Target.GetType().Name == "QuestNodeView" || d.Target.GetType().ToString().Contains("QuestNodeView")) ||
                    ReferenceEquals(d.Target, currentValidView)
                )
                .ToArray();

            del = null;

            if (cleanList.Length > 0)
            {
                del = (Action<TDelegate>)Delegate.Combine(cleanList);
            }
        }
    }
}