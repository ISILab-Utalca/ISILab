using System.Threading;
using UnityEngine;

namespace ISILab.LBS.Plugin.Core.AI.Assistant
{
    public interface IAssistantThreaded
    {
        /// <summary>
        /// Call whenever the cancel token is requesting the cancellation
        /// </summary>
        public abstract void OnTaskCancelled();


        /// <summary>
        /// Checks if the provided cancellation token has been triggered, and if so,
        /// automatically calls the <see cref="IAssistantThreaded.OnTaskCancelled"/> implementation.
        /// </summary>
        /// <remarks>
        /// Use this method like:
        /// <code>
        ///     if(((IAssistantThreaded)this).CheckPendingCancel(this, token))
        ///     {
        ///       return;
        ///     }
        /// </code>
        /// All logic for handling cancellation should reside within the 
        /// <see cref="IAssistantThreaded.OnTaskCancelled"/> implementation.
        /// </remarks>
        /// <param name="InterfaceOwner">The class instance implementing <see cref="IAssistantThreaded"/> where the token check is performed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> associated with the current task.</param>
        /// <returns><c>true</c> if the token was cancelled and <see cref="OnTaskCancelled"/> was invoked; otherwise <c>false</c>.</returns>
        public bool CheckPendingCancel(object InterfaceOwner, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested) 
                return false;

            ((IAssistantThreaded)InterfaceOwner).OnTaskCancelled();
            return true;
        }
    }
}
