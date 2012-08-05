using System;

namespace Engine.Serialization
{
    /// <summary>
    /// Thrown by the <see cref="Packet"/> class on invalid reads.
    /// </summary>
    [Serializable]
    public sealed class PacketException : Exception
    {
        /// <summary>
        /// Creates a new packet exception.
        /// </summary>
        /// <param name="message">The message associated with the exception.</param>
        public PacketException(string message) : base(message) { }

        /// <summary>
        /// Creates a new packet exception.
        /// </summary>
        /// <param name="message">The message associated with the exception.</param>
        /// <param name="inner">The inner exception, that triggered this one.</param>
        public PacketException(string message, Exception inner) : base(message, inner) { }
    }
}
