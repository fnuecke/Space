using System;
using System.Net;

namespace Engine.Network
{
    /// <summary>
    /// Interface for all (network) protocols.
    /// </summary>
    public interface IProtocol : IDisposable
    {
        /// <summary>
        /// Register here to be notified of incoming data packets.
        /// </summary>
        event EventHandler Data;

        /// <summary>
        /// Keeps track of some network related statistics.
        /// </summary>
        IProtocolInfo Information { get; }
        
        /// <summary>
        /// Inject a message, handle it like it was received via the protocol itself.
        /// This method is thread safe.
        /// </summary>
        /// <param name="buffer">the data to inject.</param>
        /// <param name="remote">the remote host the message was received from.</param>
        void Inject(byte[] buffer, IPEndPoint remote);
        
        /// <summary>
        /// Drive the network connection. This must be called regularly
        /// to ensure a proper network flow.
        /// </summary>
        void Receive();
    }
}
