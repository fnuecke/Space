using System;
using System.Net;
using Engine.Serialization;

namespace Engine.Network
{
    /// <summary>
    /// Notifies of a connection that died. The <code>timeout</code> parameter tells
    /// if this was due to a timeout or not.
    /// </summary>
    /// <param name="remote">the remote machine that got disconnected (either voluntarily or due to timeout).</param>
    public delegate void TimeoutEventHandler(IPEndPoint remote);

    /// <summary>
    /// Notifies when data is received from a remote machine.
    /// </summary>
    /// <param name="remote">the remote machine that send the data.</param>
    /// <param name="data">the data that was received.</param>
    /// <returns>whether the received data was <em>successfully</em> handled.</returns>
    public delegate bool DataEventHandler(IPEndPoint remote, Packet data);

    public interface IProtocol : IDisposable
    {

        /// <summary>
        /// Register here to be notified of messages timing out (failed to deliver acked message).
        /// </summary>
        event TimeoutEventHandler MessageTimeout;

        /// <summary>
        /// Register here to be notified of incoming data packets.
        /// </summary>
        event DataEventHandler Data;

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
        /// </summary>
        /// <param name="buffer">the data to inject.</param>
        /// <param name="remote">the remote host the message was received from.</param>
        void Inject(byte[] buffer, IPEndPoint remote);

    }
}
