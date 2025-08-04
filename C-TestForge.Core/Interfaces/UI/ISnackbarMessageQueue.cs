using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.UI
{
    /// <summary>
    /// Interface for snackbar message queue
    /// </summary>
    public interface ISnackbarMessageQueue
    {
        /// <summary>
        /// Enqueues a notification message for display in a snackbar
        /// </summary>
        /// <param name="message">Message to display</param>
        void Enqueue(string message);

        /// <summary>
        /// Enqueues a notification message for display in a snackbar with custom duration
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="duration">Duration to display the message</param>
        void Enqueue(string message, TimeSpan duration);

        /// <summary>
        /// Enqueues a notification message with action for display in a snackbar
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="actionContent">Content for the action button</param>
        /// <param name="actionHandler">Action to execute when button is clicked</param>
        void Enqueue(string message, object actionContent, Action actionHandler);
    }
}
