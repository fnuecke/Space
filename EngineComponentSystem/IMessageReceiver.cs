using System;
using System.Collections.Generic;

namespace Engine.ComponentSystem
{
    /// <summary>
    /// Interface for messaging-enabled objects.
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// A list of message types this receiver is interested in.
        /// </summary>
        public IEnumerable<Type> RelevantMessageTypes { get; }

        /// <summary>
        /// Handles a specific message.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to handle.</param>
        public void Receive<T>(ref T message) where T : struct;
    }
}
