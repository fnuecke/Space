using System;
using System.Net;
using Engine.Serialization;

namespace Engine.Network
{
    /// <summary>
    /// Event args used for protocol events including data.
    /// </summary>
    public class ProtocolDataEventArgs : EventArgs
    {
        /// <summary>
        /// The remote end point the event applies to.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// The data we received.
        /// </summary>
        public IReadablePacket Data { get; private set; }

        /// <summary>
        /// Creates a new protocol data event argument wrapper.
        /// </summary>
        /// <param name="endPoint">The endpoint that triggered the event.</param>
        /// <param name="data">The data associated with the event.</param>
        public ProtocolDataEventArgs(IPEndPoint endPoint, IReadablePacket data)
        {
            RemoteEndPoint = endPoint;
            Data = data;
        }
    }
}
