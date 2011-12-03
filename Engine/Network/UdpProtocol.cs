using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Engine.Serialization;
using Engine.Util;

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
        #region Constants

        /// <summary>
        /// Lookup table for priority to udp poll rate conversion.
        /// </summary>
        private static readonly Dictionary<PacketPriority, uint> PollRates = new Dictionary<PacketPriority, uint>()
        {
            { PacketPriority.None , 0 },
            { PacketPriority.Lowest, 5000 },
            { PacketPriority.Low, 500 },
            { PacketPriority.Medium, 100 },
            { PacketPriority.High, 50 }
        };

        /// <summary>
        /// This is used to determine the local loopback address to use for a new
        /// instance.
        /// </summary>
        private static BitArray boundPorts = new BitArray(ushort.MaxValue);

        /// <summary>
        /// Lookup table the instances add themselves to. This essentially fakes
        /// a tiny LAN which only exists inside the running program.
        /// </summary>
        private static Dictionary<ushort, UdpProtocol> loopbacksByPort = new Dictionary<ushort, UdpProtocol>();

        #endregion

        #region Events

        /// <summary>
        /// Register here to be notified of connections being closed.
        /// </summary>
        public event EventHandler MessageTimeout;

        /// <summary>
        /// Register here to be notified of incoming data packets.
        /// </summary>
        public event EventHandler Data;

        #endregion

        #region Properties

        /// <summary>
        /// Keeps track of some network related statistics.
        /// </summary>
        public IProtocolInfo Information { get { return info; } }

        /// <summary>
        /// The local loopback address for this protocol. Use this to send
        /// messages to from another protocol.
        /// </summary>
        public IPEndPoint Loopback { get; private set; }

        /// <summary>
        /// The ping frequency, i.e. how often (every <c>PingFrequency</c> milliseconds)
        /// to send pings to known remote hosts. This is used to detect failed connections
        /// as well as accumulate ping values on near-idle connections.
        /// </summary>
        public uint PingFrequency { get; set; }

        /// <summary>
        /// The timeout (in milliseconds) after which to stop resending a packet and instead dispatching a timeout event.
        /// </summary>
        public uint Timeout { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The header for the protocol used via this connection (serves to identify relevant packages).
        /// </summary>
        private UdpMessageFactory messages;

        /// <summary>
        /// The actual underlying UDP socket.
        /// </summary>
        private UdpClient udp;

        /// <summary>
        /// Actual value for protocol info.
        /// </summary>
        private ProtocolInfo info = new ProtocolInfo(60);

        /// <summary>
        /// Currently open connections (i.e. packet was sent to or received from this remote end point).
        /// </summary>
        private Dictionary<long, UdpRemoteInfo> connections = new Dictionary<long, UdpRemoteInfo>();

        /// <summary>
        /// Messages we're currently waiting for an ack for (resend as prioritized, timeout as configured).
        /// </summary>
        private Dictionary<long, UdpPendingMessage> awaitingAck = new Dictionary<long, UdpPendingMessage>();

        /// <summary>
        /// Last time we since our collective ping.
        /// </summary>
        private long lastPing = 0;

        #endregion

        #region Constructor / Cleanup

        /// <summary>
        /// Creates a new UDP socket listening on the given port for the given protocol.
        /// </summary>
        /// <param name="port">the port to listen on/send through.</param>
        /// <param name="protocolHeader">header of the used protocol (filter packages).</param>
        public UdpProtocol(ushort port, byte[] protocolHeader)
        {
            // Create our actual udp socket.
            udp = new UdpClient(port);

            Loopback = new IPEndPoint(IPAddress.Loopback, port);
            loopbacksByPort[port] = this;
            boundPorts[port] = true;

            this.messages = new UdpMessageFactory(protocolHeader);
            this.Timeout = 10000;
            this.PingFrequency = 1000;
        }

        /// <summary>
        /// Close this connection for good. This class should not be used again after calling this.
        /// </summary>
        public void Dispose()
        {
            ushort port = (ushort)((IPEndPoint)udp.Client.LocalEndPoint).Port;
            loopbacksByPort.Remove(port);
            boundPorts[port] = false;

            connections.Clear();
            awaitingAck.Clear();
            udp.Close();
        }

        /// <summary>
        /// Drive the network connection by receiving available packets,
        /// and resending yet unacked messages.
        /// </summary>
        public void Update()
        {
            Receive();
            Flush();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the ping for the given remote end point, if possible.
        /// </summary>
        /// <param name="remote">return the averaged ping to the remote host, or 0 if unknown.</param>
        public int GetPing(IPEndPoint remote)
        {
            UdpRemoteInfo connection = GetConnection(remote);
            if (connection != null)
            {
                return connection.Ping;
            }
            return -1;
        }

        /// <summary>
        /// Send some data to a remote host.
        /// </summary>
        /// <param name="packet">the data to send.</param>
        /// <param name="remote">the remote host to send it to.</param>
        /// <param name="priority">the priority to send the packet with.</param>
        public void Send(Packet packet, IPEndPoint remote, PacketPriority priority)
        {
            if (remote == null)
            {
                throw new ArgumentNullException("remote");
            }
            uint pollRate = PollRates[priority];
            if (pollRate > 0)
            {
                int messageNumber;
                Packet message = messages.MakeAcked(packet, out messageNumber);
                awaitingAck.Add(messageNumber, new UdpPendingMessage(message, remote, pollRate));
            }
            else
            {
                Packet message = messages.MakeUnacked(packet);
                info.Outgoing(message.Length, TrafficTypes.Data);
                Send(message.Buffer, message.Length, remote);
            }
        }

        /// <summary>
        /// Inject a message, handle it like it was received via the protocol itself.
        /// </summary>
        /// <param name="buffer">the data to inject.</param>
        /// <param name="remote">the remote host the message was received from.</param>
        public void Inject(byte[] buffer, IPEndPoint remote)
        {
            Inject(buffer, buffer.Length, remote);
        }

        /// <summary>
        /// Inject a message, handle it like it was received via the protocol itself.
        /// </summary>
        /// <param name="buffer">the data to inject.</param>
        /// <param name="remote">the remote host the message was received from.</param>
        public void Inject(byte[] buffer, int length, IPEndPoint remote)
        {
            // Can we parse it? The values below are set as context dictates.
            SocketMessage type;
            int messageNumber;
            Packet data;

            // Did we get a valid message.
            if (!messages.ParseMessage(buffer, out type, out messageNumber, out data))
            {
                info.Incoming(buffer.Length, TrafficTypes.Invalid);
                return;
            }

            // Get the connection associated with this remote machine. Only create a new one
            // if it's a data packet.
            UdpRemoteInfo connection = GetConnection(remote, true);
            if (connection == null)
            {
                // We couldn't get the raw IP.
#if DEBUG
                Console.WriteLine("Udp: cannot get raw ip");
#endif
                return;
            }
            connection.LastReceived = DateTime.Now;

            // Act based on type.
            switch (type)
            {
                case SocketMessage.Ack:
                    // It's an ack.
                    info.Incoming(buffer.Length, TrafficTypes.Protocol);
                    if (awaitingAck.ContainsKey(messageNumber))
                    {
                        awaitingAck.Remove(messageNumber);
                    }
                    break;

                case SocketMessage.Acked:
                    // Acked data.
                    info.Incoming(buffer.Length, TrafficTypes.Data);
                    if (connection != null)
                    {
                        // Only handle these once successfully, as they may have been resent
                        // (ack didn't get back quick enough or failed handling before).
                        if (!connection.IsAlreadyHandled(messageNumber))
                        {
                            // Failed last time, retry because some other packet might have
                            // arrived in the meantime, changing the handler's state.
                            var dataArgs = new ProtocolDataEventArgs(remote, data);
                            OnData(dataArgs);
                            if (dataArgs.WasConsumed)
                            {
                                // Success! Remember that.
                                connection.MarkHandled(messageNumber);
                            }
                            else
                            {
                                // Failed handling, don't send ack.
#if DEBUG
                                Console.WriteLine("Udp: acked failed handling");
#endif
                                return;
                            }
                        } // else handled successfully before, resend ack.

                        // Send ack if we get here.
                        Packet ack = messages.MakeAck(messageNumber);
                        info.Outgoing(ack.Length, TrafficTypes.Protocol);
                        Send(ack.Buffer, ack.Length, remote);
                    }
                    break;

                case SocketMessage.Ping:
                    // It's a ping, send a pong.
                    info.Incoming(buffer.Length, TrafficTypes.Protocol);
                    if (data.HasInt64())
                    {
                        Packet pong = messages.MakePong(data.ReadInt64());
                        info.Outgoing(pong.Length, TrafficTypes.Protocol);
                        Send(pong.Buffer, pong.Length, remote);
                    }
                    break;

                case SocketMessage.Pong:
                    // It's a pong! Calculate latency.
                    if (data.HasInt64())
                    {
                        connection.PushPing((int)new TimeSpan((DateTime.Now.Ticks - data.ReadInt64()) / 2).TotalMilliseconds);
                    }
                    break;

                case SocketMessage.Unacked:
                    // Unacked data, this is only sent once, presumably.
                    info.Incoming(buffer.Length, TrafficTypes.Data);
                    OnData(new ProtocolDataEventArgs(remote, data));
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Drive network

        /// <summary>
        /// Check if there is any data that can be received. This can trigger OnConnect and
        /// OnData events (handled in the same thread calling this function).
        /// </summary>
        private void Receive()
        {
            //if (udp.Client != null)
            {
                var remote = new IPEndPoint(0, 0);
                while (udp.Available > 0)
                {
                    try
                    {
                        byte[] buffer = udp.Receive(ref remote);
                        Inject(buffer, remote);
                    }
#if DEBUG
                    catch (SocketException ex)
                    {
                        Console.WriteLine(ex);
                    }
#else
                    catch (SocketException)
                    {
                    }
#endif
                }
            }
        }

        /// <summary>
        /// Send or resend any acked messages we still haven't got an ack for.
        /// This can trigger MessageTimeout events.
        /// </summary>
        private void Flush()
        {
            long time = DateTime.Now.Ticks;
            List<IPEndPoint> toRemove = new List<IPEndPoint>();
            foreach (var message in new List<UdpPendingMessage>(awaitingAck.Values))
            {
                if (new TimeSpan(time - message.timeCreated).TotalMilliseconds > Timeout)
                {
                    // This connection timed out. Mark for closing.
                    toRemove.Add(message.remote);
                }
                else if (new TimeSpan(time - message.lastSent).TotalMilliseconds > message.pollRate)
                {
                    info.Outgoing(message.data.Length, TrafficTypes.Data);
                    try
                    {
                        Send(message.data.Buffer, message.data.Length, message.remote);
                        // Incrementally stretch the time between resends, to avoid flooding
                        // the adapter for low initial pollrates.
                        if (message.lastSent != 0)
                        {
                            message.pollRate += message.pollRate;
                        }
                        message.lastSent = time;
                    }
                    catch (SocketException)
                    {
                        toRemove.Add(message.remote);
                    }
                }
            }

            // Do actual removal after iteration, as this modifies the collection.
            foreach (var remote in toRemove)
            {
#if DEBUG
                Console.WriteLine("Udp: timeout for " + remote.ToString());
#endif
                RemoveConnection(remote);
                OnTimeout(new ProtocolEventArgs(remote));
            }

            // Do pings for known connections once in a while.
            if (new TimeSpan(time - lastPing).TotalMilliseconds > PingFrequency)
            {
                Packet message = messages.MakePing();
                foreach (var item in connections.Values)
                {
                    // Only ping remote hosts that sent us a message (replied) recently.
                    if ((DateTime.Now - item.LastReceived).TotalMilliseconds < PingFrequency * 2)
                    {
                        info.Outgoing(message.Length, TrafficTypes.Protocol);
                        Send(message.Buffer, message.Length, item.Remote);
                    }
                }
                lastPing = time;
            }
        }

        #endregion

        #region Utility methods

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

        private UdpRemoteInfo GetConnection(IPEndPoint remote, bool dueToReceived = false)
        {
            // Get remote IP address in raw format.
            long remoteIp;
            if (!GetRawIp(remote.Address, out remoteIp))
            {
                return null;
            }

            // OK, if we may and need, create a connection object.
            if (!connections.ContainsKey(remoteIp))
            {
                if (dueToReceived)
                {
                    connections.Add(remoteIp, new UdpRemoteInfo(remote, DateTime.Now));
                }
                else
                {
                    return null;
                }
            }

            // And return it.
            return connections[remoteIp];
        }
        
        private void Send(byte[] dgram, int bytes, IPEndPoint endPoint)
        {
            if (IPAddress.IsLoopback(endPoint.Address) &&
                loopbacksByPort.ContainsKey((ushort)endPoint.Port))
            {
                UdpProtocol remote = loopbacksByPort[(ushort)endPoint.Port];
                remote.Inject(dgram, bytes, Loopback);
            }
            else
            {
                udp.Send(dgram, bytes, endPoint);
            }
        }

        private void OnData(ProtocolDataEventArgs e)
        {
            if (Data != null)
            {
                Data(this, e);
            }
        }

        private void OnTimeout(ProtocolEventArgs e)
        {
            if (MessageTimeout != null)
            {
                MessageTimeout(this, e);
            }
        }

        #endregion
    }

    #region Utility classes

    [Flags]
    enum SocketMessage
    {
        /// <summary>
        /// Used as a default value.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// The message is an ack for a message we sent to the remote machine.
        /// </summary>
        Ack = 1,

        /// <summary>
        /// This message requires us to send an ack.
        /// </summary>
        Acked = 2,

        /// <summary>
        /// This message does not require us to send an ack.
        /// </summary>
        Unacked = 4,

        /// <summary>
        /// Used to measure latencies to known hosts.
        /// </summary>
        Ping = 8,

        /// <summary>
        /// Reply to a ping.
        /// </summary>
        Pong = 16,

        /// <summary>
        /// Indicates that further data in this packet is gzip compressed.
        /// </summary>
        Compressed = 32
    }

    /// <summary>
    /// Utility class for serializing / deserializing messages.
    /// </summary>
    sealed class UdpMessageFactory
    {
        /// <summary>
        /// We'll just use the same key engine globally, as this isn't meant
        /// as a waterproof security anyways. Just make it easier to stay
        /// honest, so to say ;)
        /// </summary>
        private static readonly byte[] key = new byte[] { 58, 202, 84, 179, 32, 50, 8, 252, 238, 91, 233, 209, 25, 203, 183, 237, 33, 159, 103, 243, 93, 46, 67, 2, 169, 100, 96, 33, 196, 195, 244, 113 };

        /// <summary>
        /// Globally used initial vector.
        /// </summary>
        private static readonly byte[] vector = new byte[] { 112, 155, 187, 151, 110, 190, 166, 5, 137, 147, 104, 79, 199, 129, 24, 187 };

        /// <summary>
        /// Cryptography instance we'll use for mangling our packets.
        /// </summary>
        private static readonly SimpleCrypto crypto = new SimpleCrypto(key, vector);

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

        public Packet MakePing()
        {
            Packet wrapper = new Packet(sizeof(long));
            wrapper.Write(DateTime.Now.Ticks);
            return MakeMessage(SocketMessage.Ping, wrapper);
        }

        public Packet MakePong(long ping)
        {
            Packet wrapper = new Packet(sizeof(long));
            wrapper.Write(ping);
            return MakeMessage(SocketMessage.Pong, wrapper);
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
        /// <param name="message">the data to parse.</param>
        /// <param name="type">the type of the parsed data.</param>
        /// <param name="messageNumber">the number of the parsed message.</param>
        /// <param name="data">the data parsed from the message.</param>
        /// <returns>if parsing was successful or not.</returns>
        public bool ParseMessage(byte[] message, out SocketMessage type, out int messageNumber, out Packet data)
        {
            // Satisfy outs.
            type = SocketMessage.Invalid;
            messageNumber = -1;
            data = null;

            // Check the header.
            if (IsHeaderValid(message))
            {
                var packet = new Packet(crypto.Decrypt(message, header.Length, message.Length - header.Length));

                // Get the type of the message.
                if (!packet.HasByte())
                {
                    return false;
                }
                type = (SocketMessage)packet.ReadByte();

                // Get the message body.
                Packet body;
                if ((type & SocketMessage.Compressed) > 0)
                {
                    type &= ~SocketMessage.Compressed;
                    if (!packet.HasByteArray())
                    {
                        return false;
                    }
                    byte[] compressed = packet.ReadByteArray();
                    body = new Packet(SimpleCompression.Decompress(compressed));
                }
                else
                {
                    if (!packet.HasPacket())
                    {
                        return false;
                    }
                    body = packet.ReadPacket();
                }

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
                else if (type == SocketMessage.Unacked || type == SocketMessage.Ping || type == SocketMessage.Pong)
                {
                    data = body;
                }

                return true;
            }

            return false;
        }

        private Packet MakeMessage(SocketMessage type, Packet packet)
        {
            byte[] data = null;
            int length = 0;
            if (packet != null)
            {
                data = packet.Buffer;
                length = packet.Length;
                // If packets are large, try compressing them, see if it helps.
                // Only start after a certain size. General overhead for gzip
                // seems to be around 130byte, so make sure we're well beyond that.
                if (packet.Length > 256)
                {
                    byte[] compressed = SimpleCompression.Compress(packet.Buffer, packet.Length);
                    if (compressed.Length < packet.Length)
                    {
                        type |= SocketMessage.Compressed;
                        data = compressed;
                        length = compressed.Length;
                    }
                }
            }
            Packet wrapper = new Packet(5 + length);
            wrapper.Write((byte)type);
            wrapper.Write(data);

            byte[] encrypted = crypto.Encrypt(wrapper.Buffer);
            byte[] final = new byte[header.Length + encrypted.Length];
            header.CopyTo(final, 0);
            encrypted.CopyTo(final, header.Length);
            return new Packet(final);
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
        /// The average ping of this connection.
        /// </summary>
        public int Ping { get { return pingAverage.Mean(); } }

        /// <summary>
        /// The remote address this info is about.
        /// </summary>
        public IPEndPoint Remote { get; private set; }

        /// <summary>
        /// The last time we received a packet from this host.
        /// </summary>
        public DateTime LastReceived { get; set; }

        /// <summary>
        /// List of messages already handled recently.
        /// </summary>
        private HashSet<int> handledMessages = new HashSet<int>();

        /// <summary>
        /// Average ping to this client, i.e. half round trip time for acked messages,
        /// over the last 20 messages.
        /// </summary>
        private Sampling pingAverage = new Sampling(5, 20);

        /// <summary>
        /// Creates a new instance to track the state for an endpoint.
        /// </summary>
        public UdpRemoteInfo(IPEndPoint remote, DateTime lastReceived)
        {
            this.LastReceived = lastReceived;
            this.Remote = remote;
        }

        /// <summary>
        /// Check if a certain message has already been handled, so we won't trigger twice for the same data.
        /// </summary>
        /// <param name="messageNumber">the message to check.</param>
        /// <returns><code>true</code> if <code>MarkHandled(messageNumber)</code> has been called before.</returns>
        public bool IsAlreadyHandled(int messageNumber)
        {
            return handledMessages.Contains(messageNumber);
        }

        /// <summary>
        /// Mark a message as handled, meaning it won't be broadcast upon further reception.
        /// </summary>
        /// <param name="messageNumber">the number of the message to mark as handled.</param>
        public void MarkHandled(int messageNumber)
        {
            handledMessages.Add(messageNumber);
        }

        /// <summary>
        /// Push a new ping value for this connection.
        /// </summary>
        /// <param name="pingValue">the value of the ping to push.</param>
        public void PushPing(int pingValue)
        {
            pingAverage.Put(pingValue);
        }
    }

    #endregion
}
