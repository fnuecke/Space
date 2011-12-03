using System;
using System.Net;
using Engine.Serialization;

namespace Engine.Network
{
    /// <summary>
    /// A list of possible packet priorities.
    /// </summary>
    public enum PacketPriority
    {
        /// <summary>
        /// The packet does not necessarily have to be delivered. For example,
        /// it will not be retransmitted if the first delivery attempt fails.
        /// This may be used for repetitive requests, such as synchronizing the
        /// game's speed between server and client.
        /// </summary>
        None,

        /// <summary>
        /// The packet has no timely relevance but should be delivered at some
        /// point. This might be used for chat messages or connection liveness
        /// tests.
        /// </summary>
        Lowest,

        /// <summary>
        /// The packet must reach its target, but it can take some time. This
        /// might be a join request, or a notification that a player has left
        /// the game.
        /// </summary>
        Low,

        /// <summary>
        /// The packet should be delivered within a reasonable time-frame. For
        /// example, a request for a re-transmit of the game state, or that
        /// retransmit itself may fall in this category.
        /// </summary>
        Medium,

        /// <summary>
        /// The packet should be delivered as quickly as possible. These can
        /// be player actions which are immediately visible, such as movement
        /// or shots fired, for example.
        /// </summary>
        High
    }

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
        /// Keeps track of some network related statistics.
        /// </summary>
        IProtocolInfo Information { get; }
        
        /// <summary>
        /// Get the ping for the given remote end point, if possible.
        /// </summary>
        /// <param name="remote">return the averaged ping to the remote host, or 0 if unknown.</param>
        int GetPing(IPEndPoint remote);

        /// <summary>
        /// Send some data to a remote host.
        /// </summary>
        /// <param name="packet">the data to send.</param>
        /// <param name="remote">the remote host to send it to.</param>
        /// <param name="priority">the priority to send the message with.</param>
        void Send(Packet packet, IPEndPoint remote, PacketPriority priority);

        /// <summary>
        /// Inject a message, handle it like it was received via the protocol itself.
        /// This method is thread safe.
        /// </summary>
        /// <param name="buffer">the data to inject.</param>
        /// <param name="remote">the remote host the message was received from.</param>
        void Inject(byte[] buffer, IPEndPoint remote);
        
        /// <summary>
        /// Drive the network connection by receiving available packets,
        /// and resending yet unacked messages. This must be called regularly
        /// to ensure a proper network flow.
        /// </summary>
        void Update();
    }
}
