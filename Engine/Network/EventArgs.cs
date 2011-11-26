using System;
using System.Net;
using Engine.Serialization;

namespace Engine.Network
{
    /// <summary>
    /// Event args used for <see cref="Engine.Network.IProtocol#MessageTimeout"/>.
    /// </summary>
    public class ProtocolEventArgs : EventArgs
    {
        /// <summary>
        /// The remote end point the event applies to.
        /// </summary>
        public IPEndPoint Remote { get; private set; }

        public ProtocolEventArgs(IPEndPoint remote)
        {
            this.Remote = remote;
        }
    }

    /// <summary>
    /// Event args used for <see cref="Engine.Network.IProtocol#Data"/>.
    /// </summary>
    public class ProtocolDataEventArgs : ProtocolEventArgs
    {
        /// <summary>
        /// The data we received.
        /// </summary>
        public Packet Data { get; private set; }

        /// <summary>
        /// Tells whether the event was handled by a listener.
        /// </summary>
        public bool WasConsumed { get; private set; }

        public ProtocolDataEventArgs(IPEndPoint remote, Packet data)
            : base(remote)
        {
            this.Data = data;
        }

        /// <summary>
        /// Called by listeners to signal the event was handled.
        /// </summary>
        public void Consume()
        {
            WasConsumed = true;
        }
    }
}
