using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISnackbarMessageQueue = C_TestForge.Core.Interfaces.UI.ISnackbarMessageQueue;

namespace C_TestForge.UI.Services
{
    /// <summary>
    /// Adapter for Material Design's SnackbarMessageQueue
    /// </summary>
    public class SnackbarMessageQueueAdapter : ISnackbarMessageQueue
    {
        private readonly SnackbarMessageQueue _messageQueue;

        public SnackbarMessageQueueAdapter(SnackbarMessageQueue messageQueue)
        {
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public void Enqueue(string message)
        {
            _messageQueue.Enqueue(message);
        }

        public void Enqueue(string message, TimeSpan duration)
        {
            _messageQueue.Enqueue(message, null, null, null, false, false, duration);
        }

        public void Enqueue(string message, object actionContent, Action actionHandler)
        {
            _messageQueue.Enqueue(message, actionContent, actionHandler);
        }
    }
}
