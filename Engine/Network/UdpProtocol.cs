using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Engine.Serialization;

namespace Engine.Network
{

    /// <summary>
    /// Base class for UDP based communication.
    /// </summary>
    /// <remarks>
    /// Implements basic reliability features via custom acks, resending and timeouts.
    /// </remarks>
    public sealed class UdpProtocol : IProtocol
    {

        /// <summary>
        /// Register here to be notified of connections being closed.
        /// </summary>
        public event TimeoutEventHandler MessageTimeout;

        /// <summary>
        /// Register here to be notified of incoming data packets.
        /// </summary>
        public event DataEventHandler Data;

        /// <summary>
        /// The timeout (in milliseconds) after which to stop resending a packet and instead dispatching a timeout event.
        /// </summary>
        public uint Timeout { get; set; }

        /// <summary>
        /// The header for the protocol used via this connection (serves to identify relevant packages).
        /// </summary>
        private UdpMessageFactory messages;

        /// <summary>
        /// The actual underlying UDP socket.
        /// </summary>
        private UdpClient udp;

        /// <summary>
        /// Currently open connections (i.e. packet was sent to or received from this remote end point).
        /// </summary>
        private Dictionary<long, UdpRemoteInfo> connections = new Dictionary<long, UdpRemoteInfo>();

        /// <summary>
        /// Messages we're currently waiting for an ack for (resend as prioritized, timeout as configured).
        /// </summary>
        private Dictionary<long, UdpPendingMessage> awaitingAck = new Dictionary<long, UdpPendingMessage>();

        /// <summary>
        /// Creates a new UDP socket listening on the given port for the given protocol.
        /// </summary>
        /// <param name="port">the port to listen on/send through.</param>
        /// <param name="protocolHeader">header of the used protocol (filter packages).</param>
        public UdpProtocol(ushort port, byte[] protocolHeader)
        {
            this.messages = new UdpMessageFactory(protocolHeader);
            this.Timeout = 5000;

            // Create our actual udp socket.
            udp = new UdpClient(port);
        }

        /// <summary>
        /// Close this connection for good. This class should not be used again after calling this.
        /// </summary>
        public void Dispose()
        {
            connections.Clear();
            awaitingAck.Clear();
            udp.Close();
        }

        /// <summary>
        /// Check if there is any data that can be received. This can trigger OnConnect and
        /// OnData events (handled in the same thread calling this function).
        /// </summary>
        public void Receive()
        {
            var remote = new IPEndPoint(0, 0);
            while (udp.Available > 0)
            {
                byte[] buffer = udp.Receive(ref remote);
                Inject(buffer, remote);
            }
        }

        /// <summary>
        /// Send some data to a remote host.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <param name="remote">the remote host to send it to.</param>
        /// <param name="pollRate">if this is set, it means the message should be acked,
        /// and this is the rate in millisecond in which to resend the message while it
        /// didn't get its ack. The highest accuracy for this is the rate with which
        /// <code>Flush()</code> is called.</param>
        public void Send(Packet data, IPEndPoint remote, uint pollRate = 0)
        {
            if (pollRate > 0)
            {
                int messageNumber;
                Packet message = messages.MakeAcked(data, out messageNumber);
                awaitingAck.Add(messageNumber, new UdpPendingMessage(message, remote, pollRate));
            }
            else
            {
                Packet message = messages.MakeUnacked(data);
                udp.Send(message.Buffer, message.Length, remote);
            }
        }

        /// <summary>
        /// Send or resend any acked messages we still haven't got an ack for.
        /// This can trigger MessageTimeout events.
        /// </summary>
        public void Flush()
        {
            long time = DateTime.Now.Ticks;
            List<IPEndPoint> toRemove = new List<IPEndPoint>();
            foreach (var message in awaitingAck.Values)
            {
                if (new TimeSpan(time - message.timeCreated).TotalMilliseconds > Timeout)
                {
                    // This connection timed out. Mark for closing.
                    toRemove.Add(message.remote);
                }
                else if (new TimeSpan(time - message.lastSent).TotalMilliseconds > message.pollRate)
                {
                    message.lastSent = time;
                    udp.Send(message.data.Buffer, message.data.Length, message.remote);
                }
	        }
            // Do actual removal after iteration, as this modifies the collection.
            foreach (var remote in toRemove)
            {
                RemoveConnection(remote);
                OnTimeout(remote);
            }
        }

        public void Inject(byte[] buffer, IPEndPoint remote)
        {
            // Can we parse it? The values below are set as context dictates.
            Packet packet = new Packet(buffer);
            SocketMessage type;
            int messageNumber;
            Packet data;
            if (!messages.ParseMessage(packet, out type, out messageNumber, out data))
            {
                return;
            }

            // Get the connection associated with this remote machine. Only create a new one
            // if it's a data packet.
            UdpRemoteInfo connection = null;
            if (!GetConnection(remote, out connection))
            {
                // We couldn't get the raw IP.
                return;
            }

            // Act based on type.
            switch (type)
            {
                case SocketMessage.Invalid:
                    break;
                case SocketMessage.Ack:
                    // It's an ack.
                    awaitingAck.Remove(messageNumber);
                    break;
                case SocketMessage.Acked:
                    // Acked data.
                    if (connection != null)
                    {

                        // Only handle these once, as they may have been resent (ack didn't get back quick enough).
                        bool result;
                        if (connection.IsAlreadyHandled(messageNumber, out result))
                        {
                            // If this wasn't handled before, don't send an ack now, either.
                            if (!result)
                            {
                                return;
                            }
                        }
                        else
                        {
                            connection.MarkHandled(messageNumber, OnData(remote, data));
                        }

                        Packet ack = messages.MakeAck(messageNumber);
                        udp.Send(ack.Buffer, ack.Length, remote);
                    }
                    break;
                case SocketMessage.Unacked:
                    // Unacked data, this is only sent once, presumably.
                    OnData(remote, data);
                    break;
                default:
                    break;
            }
            if (type == SocketMessage.Ack)
            {
            }
            else if (type == SocketMessage.Unacked)
            {
            }
            else if (type == SocketMessage.Acked)
            {
            }
        }

        private void RemoveConnection(IPEndPoint remote)
        {
            long remoteIp;
            if (GetRawIp(remote.Address, out remoteIp))
            {
                if (connections.ContainsKey(remoteIp))
                {
                    connections.Remove(remoteIp);
                }

                List<long> toRemove = new List<long>();
                foreach (var pair in awaitingAck)
                {
                    if (pair.Value.remote.Equals(remote))
                    {
                        toRemove.Add(pair.Key);
                    }
                }
                foreach (var messageNumber in toRemove)
                {
                    awaitingAck.Remove(messageNumber);
                }
            }
        }

        private static bool GetRawIp(IPAddress address, out long rawAddress)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                rawAddress = BitConverter.ToInt32(address.GetAddressBytes(), 0);
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                rawAddress = BitConverter.ToInt64(address.GetAddressBytes(), 0);
            }
            else
            {
                rawAddress = 0; //< Satisfy outs.
                return false;
            }
            return true;
        }

        private bool GetConnection(IPEndPoint remote, out UdpRemoteInfo connection)
        {
            // Satisfy outs.
            connection = null;

            // Get remote IP address in raw format.
            long remoteIp;
            if (!GetRawIp(remote.Address, out remoteIp))
            {
                return false;
            }

            // OK, if we may and need, create a connection object.
            if (!connections.ContainsKey(remoteIp))
            {
                connections.Add(remoteIp, new UdpRemoteInfo());
            }

            // And return it.
            connection = connections[remoteIp];

            return true;
        }

        private bool OnData(IPEndPoint remote, Packet packet)
        {
            if (Data != null)
            {
                return Data(remote, packet);
            }
            else
            {
                return false;
            }
        }

        private void OnTimeout(IPEndPoint remote)
        {
            if (MessageTimeout != null)
            {
                MessageTimeout(remote);
            }
        }
    }

    enum SocketMessage
    {
        /// <summary>
        /// Used as a default value.
        /// </summary>
        Invalid,

        /// <summary>
        /// The message is an ack for a message we sent to the remote machine.
        /// </summary>
        Ack,

        /// <summary>
        /// This message requires us to send an ack.
        /// </summary>
        Acked,

        /// <summary>
        /// This message does not require us to send an ack.
        /// </summary>
        Unacked
    }

    /// <summary>
    /// Utility class for serializing / deserializing messages.
    /// </summary>
    sealed class UdpMessageFactory
    {

        /// <summary>
        /// The header we use for all messages.
        /// </summary>
        private byte[] header;

        /// <summary>
        /// The number of the next acked packet we'll send.
        /// </summary>
        private int nextMessageNumber;

        public UdpMessageFactory(byte[] header)
        {
            this.header = header;

            // Set the frame number to some random value. This way we can
            // be pretty sure we're not getting into the trouble of someone
            // ignoring us because of an earlier connection from this machine.
            nextMessageNumber = new Random().Next();
        }

        /// <summary>
        /// Create a message that acks another.
        /// </summary>
        /// <param name="messageNumber">the message to send an ack for.</param>
        /// <returns>the data representing the message.</returns>
        public Packet MakeAck(int messageNumber)
        {
            Packet packet = new Packet(4);
            packet.Write(messageNumber);
            return MakeMessage(SocketMessage.Ack, packet);
        }

        /// <summary>
        /// Create an acked message, i.e. a message we will want an ack for.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <param name="messageNumber">the message number we'll be using.</param>
        /// <returns>the data representing the message.</returns>
        public Packet MakeAcked(Packet packet, out int messageNumber)
        {
            messageNumber = nextMessageNumber++;
            Packet wrapper = new Packet(8 + packet.Length);
            wrapper.Write(messageNumber);
            wrapper.Write(packet);
            return MakeMessage(SocketMessage.Acked, wrapper);
        }

        /// <summary>
        /// Create an unacked message, i.e. one that we'll only send once and don't need an ack for.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <returns>the data representing the message.</returns>
        public Packet MakeUnacked(Packet packet)
        {
            return MakeMessage(SocketMessage.Unacked, packet);
        }

        /// <summary>
        /// Parse received data to check if it's a message we can handle, and parse it.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="type"></param>
        /// <param name="messageNumber"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool ParseMessage(Packet message, out SocketMessage type, out int messageNumber, out Packet data)
        {
            // Satisfy outs.
            type = SocketMessage.Invalid;
            messageNumber = -1;
            data = null;

            // Check the header.
            if (message.HasByteArray() && IsHeaderValid(message.ReadByteArray()))
            {
                // Get the type of the message.
                if (!message.HasByte())
                {
                    return false;
                }
                type = (SocketMessage)message.ReadByte();

                // Get the message body.
                if (!message.HasPacket())
                {
                    return false;
                }
                Packet body = message.ReadPacket();

                // If it's an ack or an acked message we have a message number.
                if (type == SocketMessage.Acked || type == SocketMessage.Ack)
                {
                    if (!body.HasInt32())
                    {
                        return false;
                    }
                    messageNumber = body.ReadInt32();
                }

                // Read the data if it's a message containing any.
                if (type == SocketMessage.Acked)
                {
                    if (!body.HasPacket())
                    {
                        return false;
                    }
                    data = body.ReadPacket();
                }
                else if (type == SocketMessage.Unacked)
                {
                    data = body;
                }

                return true;
            }

            return false;
        }

        private Packet MakeMessage(SocketMessage type, Packet packet)
        {
            Packet wrapper = new Packet(9 + header.Length + (packet != null ? packet.Length : 0));
            wrapper.Write(header);
            wrapper.Write((byte)type);
            wrapper.Write(packet);
            return wrapper;
        }

        private bool IsHeaderValid(byte[] buffer)
        {
            if (buffer.Length < header.Length)
            {
                return false;
            }
            for (var i = 0; i < header.Length; ++i)
            {
                if (buffer[i] != header[i])
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Represents a message that should be acked, but hasn't been, yet.
    /// </summary>
    sealed class UdpPendingMessage
    {
        /// <summary>
        /// The data that's sent.
        /// </summary>
        public Packet data;

        /// <summary>
        /// The remote point to send it to.
        /// </summary>
        public IPEndPoint remote;

        /// <summary>
        /// The rate in milliseconds to send this message with.
        /// </summary>
        public uint pollRate;

        /// <summary>
        /// The time this message was created (for timeouts).
        /// </summary>
        public long timeCreated;

        /// <summary>
        /// The last time this message was sent.
        /// </summary>
        public long lastSent;

        public UdpPendingMessage(Packet data, IPEndPoint remote, uint pollRate)
        {
            this.data = data;
            this.remote = remote;
            this.pollRate = pollRate;
            this.timeCreated = DateTime.Now.Ticks;
            this.lastSent = 0;
        }
    }

    /// <summary>
    /// Represents a connection to a remote host, including a list of messages
    /// we already received from this client, as well 
    /// </summary>
    sealed class UdpRemoteInfo
    {
            
        /// <summary>
        /// List of messages already handled recently.
        /// </summary>
        private Dictionary<int, bool> handledMessages = new Dictionary<int, bool>();

        /// <summary>
        /// Creates a new instance to track the state for an endpoint.
        /// </summary>
        public UdpRemoteInfo()
        {
        }

        /// <summary>
        /// Check if a certain message has already been handled, so we won't trigger twice for the same data.
        /// </summary>
        /// <param name="messageNumber">the message to check.</param>
        /// <returns><code>true</code> if <code>MarkHandled(messageNumber)</code> has been called before.</returns>
        public bool IsAlreadyHandled(int messageNumber, out bool result)
        {
            if (handledMessages.ContainsKey(messageNumber))
            {
                result = handledMessages[messageNumber];
                return true;
            }
            else
            {
                result = false; //< Satisfy out.
                return false;
            }
        }

        /// <summary>
        /// Mark a message as handled, meaning it won't be broadcast upon further reception.
        /// </summary>
        /// <param name="messageNumber">the number of the message to mark as handled.</param>
        public void MarkHandled(int messageNumber, bool result)
        {
            handledMessages.Add(messageNumber, result);
        }
    }

}
