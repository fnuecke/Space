using System;

namespace Engine.Serialization
{
    /// <summary>
    /// Thrown by the <see cref="Packet"/> class on invalid reads.
    /// </summary>
    [Serializable]
    public class PacketException : Exception
    {
        public PacketException() { }
        public PacketException(string message) : base(message) { }
        public PacketException(string message, Exception inner) : base(message, inner) { }
        protected PacketException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
