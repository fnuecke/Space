using System;
using System.Net;
using Engine.Serialization;

namespace Engine.Network
{
    /// <summary>
    /// Interface for all (network) protocols.
    /// </summary>
    public interface IProtocol : IDisposable
    {

        /// <summary>
        /// Register here to be notified of messages timing out (failed to deliver acked message).
        /// </summary>
        event EventHandler MessageTimeout;

        /// <summary>
        /// Register here to be notified of incoming data packets.
        /// </summary>
        event EventHandler Data;
        
        /// <summary>
        /// Get the ping for the given remote end point, if possible.
        /// </summary>
        /// <param name="remote">return the averaged ping to the remote host, or 0 if unknown.</param>
        int GetPing(IPEndPoint remote);

        /// <summary>
        /// Send some data to a remote host.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <param name="remote">the remote host to send it to.</param>
        /// <param name="pollRate">if this is set, it means the message should be acked,
        /// and this is the rate in millisecond in which to resend the message while it
        /// didn't get its ack. The highest accuracy for this is the rate with which
        /// <code>Flush()</code> is called. This obviously only applies when the
        /// underlying protocol is unreliable.</param>
        void Send(Packet data, IPEndPoint remote, uint pollRate = 0);

        /// <summary>
        /// Inject a message, handle it like it was received via the protocol itself.
        /// This method is thread safe.
        /// </summary>
        /// <param name="buffer">the data to inject.</param>
        /// <param name="remote">the remote host the message was received from.</param>
        void Inject(byte[] buffer, IPEndPoint remote);

    }
}
