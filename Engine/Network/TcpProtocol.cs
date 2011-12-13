using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Engine.Network
{
    public sealed class TcpProtocol : AbstractProtocol
    {
        #region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Types

        /// <summary>
        /// Possible states for a TCP protocol instance.
        /// </summary>
        public enum ProtocolState
        {
            /// <summary>
            /// Invalid state, set after the object was disposed.
            /// </summary>
            None,

            /// <summary>
            /// Client: not currently connected to a server.
            /// </summary>
            Unconnected,

            /// <summary>
            /// Client: currently trying to connect to a server.
            /// </summary>
            Connecting,

            /// <summary>
            /// Client: currently connected to a server.
            /// </summary>
            Connected,

            /// <summary>
            /// Server: acting as a server.
            /// </summary>
            Server
        }

        #endregion

        #region Events

        /// <summary>
        /// If we're a client, this means our connection to the server
        /// was was established successfully. If we're a server, this
        /// means we got a new incoming connection.
        /// </summary>
        public event EventHandler<EventArgs> Connected;

        /// <summary>
        /// If we're a client, this means our connection to the server
        /// was lost. If we're a server, this means a client got
        /// disconnected.
        /// </summary>
        public event EventHandler<EventArgs> Disconnected;

        #endregion

        #region Properties

        /// <summary>
        /// Current state of this instance.
        /// </summary>
        public ProtocolState State { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// If this is running as a server, this is our connection.
        /// </summary>
        private TcpListener server;

        /// <summary>
        /// If this is running as a server, these are the connected clients.
        /// </summary>
        private List<ConnectionState> clients;

        /// <summary>
        /// If this is running as a client, this is our connection.
        /// </summary>
        private ConnectionState client;

        /// <summary>
        /// Maximum number of incoming connections allowed, if we're a server.
        /// </summary>
        private int maxConnections;

        #endregion

        #region Constructor / Cleanup

        /// <summary>
        /// Starts this instance as a client.
        /// </summary>
        /// <param name="protocolHeader">the header of our protocol.</param>
        public TcpProtocol(byte[] protocolHeader)
            : base(protocolHeader)
        {
            State = ProtocolState.Unconnected;
            var connection = new ConnectionState(new TcpClient());
            connection.Data += HandleConnectionData;
            connection.Disconnected += HandleConnectionDisconnected;

            logger.Trace("new (Client)");
        }

        /// <summary>
        /// Start this instance as a server.
        /// </summary>
        /// <param name="port">the port to listen on.</param>
        /// <param name="protocolHeader">the header of our protocol.</param>
        public TcpProtocol(ushort port, byte[] protocolHeader)
            : base(protocolHeader)
        {
            State = ProtocolState.Server;
            clients = new List<ConnectionState>();
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            logger.Trace("new (Server)");
        }

        /// <summary>
        /// Start this instance as a server with a limited number of allowed connections.
        /// </summary>
        /// <param name="port">the port to listen on.</param>
        /// <param name="maxConnections">the maximum number of allowed clients connected.</param>
        /// <param name="protocolHeader">the header of our protocol.</param>
        public TcpProtocol(ushort port, int maxConnections, byte[] protocolHeader)
            : this(port, protocolHeader)
        {
            if (maxConnections < 0)
            {
                throw new ArgumentException("maxConnections");
            }
            this.maxConnections = maxConnections;
        }

        /// <summary>
        /// Shutdown this instance, closing any open connections.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            State = ProtocolState.None;
            if (server != null)
            {
                server.Stop();
                server.Server.Close();
                foreach (var connection in clients)
                {
                    connection.Dispose();
                }
                clients.Clear();
                server = null;

                logger.Trace("delete (Server)");
            }
            if (client != null)
            {
                client.Dispose();
                client = null;

                logger.Trace("delete (Client)");
            }

        }

        #endregion

        #region Connect / Disconnect

        /// <summary>
        /// As a client, connect to the server with the given address and port.
        /// </summary>
        /// <param name="address">the address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        public void Connect(IPAddress address, int port)
        {
            if (client == null)
            {
                throw new InvalidOperationException();
            }
            if (port < 1 || port > ushort.MaxValue)
            {
                throw new ArgumentException("port");
            }
            if (State != ProtocolState.Unconnected)
            {
                Disconnect();
            }

            logger.Trace("Connecting to {0}:{1}", address, port);

            State = ProtocolState.Connecting;
            client.Connection.BeginConnect(address, port, new AsyncCallback(HandleConnect), client);
        }


        /// <summary>
        /// As a client, connect to the server at the given endpoint.
        /// </summary>
        /// <param name="address">The remote endpoint to connect to.</param>
        public void Connect(IPEndPoint endPoint)
        {
            Connect(endPoint.Address, (ushort)endPoint.Port);
        }


        /// <summary>
        /// Close a currently open connection to a server.
        /// </summary>
        public void Disconnect()
        {
            if (client == null)
            {
                throw new InvalidOperationException();
            }
            if (State != ProtocolState.Unconnected)
            {
                State = ProtocolState.Unconnected;
                client.Disconnect();
            }
        }

        #endregion

        #region Send / Receive

        /// <summary>
        /// Try receiving some packets. This will trigger the <c>Data</c> event
        /// for each received packet.
        /// </summary>
        public override void Receive()
        {
            if (server != null)
            {
                // Accept new connections while we're not maxed out.
                while ((maxConnections == 0 || clients.Count < maxConnections) && server.Pending())
                {
                    var connection = new ConnectionState(server.AcceptTcpClient());
                    connection.Data += HandleConnectionData;
                    connection.Disconnected += HandleConnectionDisconnected;
                    clients.Add(connection);
                }

                // Check all known connections for data. Iterate backwards and using
                // a counter, because disconnects detected by this can modify the
                // collection.
                for (int i = clients.Count - 1; i >= 0; --i)
                {
                    clients[i].Receive();
                }
            }
            if (client != null && State != ProtocolState.Unconnected)
            {
                client.Receive();
            }
        }

        /// <summary>
        /// Use this to implement actual sending of the given data to the given endpoint.
        /// </summary>
        /// <param name="message">the data to send/</param>
        /// <param name="bytes">the length of the data to send.</param>
        /// <param name="endPoint">the end point to send it to.</param>
        protected override void HandleSend(byte[] message, IPEndPoint endPoint)
        {
            if (server != null)
            {
                var connection = clients.Find(c => c.RemoteEndPoint.Equals(endPoint));
                if (connection != null)
                {
                    connection.Send(message);
                }
            }
            if (client != null)
            {

            }
        }

        #endregion

        #region Handling Events

        /// <summary>
        /// Got a new (asynchronous) connection.
        /// </summary>
        /// <param name="result"></param>
        private void HandleConnect(IAsyncResult result)
        {
            try
            {
                client.Connection.EndConnect(result);
                State = ProtocolState.Connected;
                OnConnected(new TcpProtocolConnectEventArgs(true));
            }
            catch (SocketException ex)
            {
                State = ProtocolState.Unconnected;
                OnConnected(new TcpProtocolConnectEventArgs(false, ex));
            }
        }

        /// <summary>
        /// Got data from a connection, parse it.
        /// </summary>
        private void HandleConnectionData(object sender, EventArgs e)
        {
            var args = (ConnectionDataEventArgs)e;
            if (!base.HandleReceive(args.Data, args.RemoteEndPoint))
            {
                DisconnectClient((ConnectionState)sender);
            }
        }

        /// <summary>
        /// Lost connection to a client, propagate it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleConnectionDisconnected(object sender, EventArgs e)
        {
            DisconnectClient((ConnectionState)sender);
        }

        /// <summary>
        /// Disconnect a client, cleaning up any internal variables.
        /// </summary>
        /// <param name="connection"></param>
        private void DisconnectClient(ConnectionState connection)
        {
            // Close the connection and clean up.
            connection.Data -= HandleConnectionData;
            connection.Disconnected -= HandleConnectionDisconnected;
            clients.Remove(connection);
            connection.Dispose();

            // Let the world know this one's a goner.
            OnDisconnected(new ProtocolEventArgs(connection.RemoteEndPoint));
        }

        #endregion

        #region Dispatching Events

        /// <summary>
        /// New connection.
        /// </summary>
        private void OnConnected(EventArgs e)
        {
            if (Connected != null)
            {
                Connected(this, e);
            }
        }

        /// <summary>
        /// Forward disconnect events.
        /// </summary>
        private void OnDisconnected(EventArgs e)
        {
            if (Disconnected != null)
            {
                Disconnected(this, e);
            }
        }

        #endregion

        #region Connection

        private sealed class ConnectionState : IDisposable
        {
            #region Events

            /// <summary>
            /// Dispatched when a message was received completely.
            /// </summary>
            public event EventHandler<EventArgs> Data;

            /// <summary>
            /// Dispatched when this connection has been lost.
            /// </summary>
            public event EventHandler<EventArgs> Disconnected;

            #endregion

            #region Properties

            /// <summary>
            /// The remote end point of this connection.
            /// </summary>
            public IPEndPoint RemoteEndPoint { get; private set; }

            /// <summary>
            /// The underlying TCP connection.
            /// </summary>
            public TcpClient Connection { get; private set; }

            #endregion

            #region Fields

            /// <summary>
            /// And the connection's stream.
            /// </summary>
            private NetworkStream stream;

            /// <summary>
            /// Buffer for reading from the stream.
            /// </summary>
            private byte[] buffer = new byte[512];

            /// <summary>
            /// Used to store any received data. This is used to build
            /// a single message (over and over).
            /// </summary>
            private MemoryStream messageStream = new MemoryStream();

            /// <summary>
            /// Used to remember the length for the message we're currently
            /// building, so we don't have to use <c>BitConverter.ToInt32()</c>
            /// all the time (when continuing to read a partially received
            /// message).
            /// </summary>
            private int messageLength = 0;

            #endregion

            #region Constructor / Cleanup

            /// <summary>
            /// Creates a new connection state for the given tcp client.
            /// </summary>
            /// <param name="tcp">the client to manage.</param>
            public ConnectionState(TcpClient tcp)
            {
                this.Connection = tcp;
                this.RemoteEndPoint = (IPEndPoint)tcp.Client.RemoteEndPoint;
                this.stream = tcp.GetStream();
            }

            public void Dispose()
            {
                messageStream.Close();
                messageStream.Dispose();
                stream.Close();
                stream.Dispose();
                try { Connection.Client.Shutdown(SocketShutdown.Both); }
                catch (Exception) { }
                Connection.Client.Dispose();
                Connection.Close();
            }

            #endregion

            #region Connect / Disconnect

            public void Disconnect()
            {
                try { Connection.Client.Shutdown(SocketShutdown.Both); }
                catch (Exception) { }
                Connection.Client.Disconnect(true);
            }
            #endregion

            #region Send / Receive

            /// <summary>
            /// Send some data to this client.
            /// </summary>
            /// <param name="buffer">the data to send.</param>
            public void Send(byte[] buffer)
            {
                try
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception)
                {
                    OnDisconnected(EventArgs.Empty);
                }
            }

            /// <summary>
            /// Try receiving messages from this client.
            /// </summary>
            public void Receive()
            {
                // Read until there's nothing left...
                while (stream.DataAvailable)
                {
                    // Get what we can fit in our buffer.
                    int readCount = stream.Read(buffer, 0, buffer.Length);
                    if (readCount <= 0)
                    {
                        // Connection died (reading 0 bytes means the connection is gone).
                        OnDisconnected(EventArgs.Empty);
                    }
                    else
                    {
                        // Got more data, parse it to see if we can finish a message.
                        Parse(0, readCount);
                    }
                }
            }

            #endregion

            #region Parsing

            /// <summary>
            /// Uses the given number of bytes from the buffer to try and finalize 
            /// a message.
            /// </summary>
            /// <param name="offset">the position in the buffer to start reading from.</param>
            /// <param name="bytes">the number of bytes to read.</param>
            private void Parse(int offset, int bytes)
            {
                if (messageStream.Length >= sizeof(int))
                {
                    // We already know our current message size. See if we can complete the
                    // message.
                    int remainingBodyBytes = messageLength - ((int)messageStream.Length - sizeof(int));
                    int bodyBytesToRead = System.Math.Min(remainingBodyBytes, bytes);
                    messageStream.Write(buffer, offset, bodyBytesToRead);

                    // We done yet?
                    if (messageStream.Length - sizeof(int) == messageLength)
                    {
                        // Yep. Wrap up a packet, propagate it, then reset and continue.
                        OnData(new ConnectionDataEventArgs(messageStream.ToArray(), RemoteEndPoint));
                    }
                }
                else
                {
                    // Message size unknown. Figure out how much more we need to read, and
                    // read at most that much.
                    int remainingSizeBytes = sizeof(int) - (int)messageStream.Length;
                    int sizeBytesToRead = System.Math.Min(bytes, remainingSizeBytes);
                    messageStream.Write(buffer, offset, sizeBytesToRead);

                    // Do we have enough data to figure out the size now?
                    if (messageStream.Length == sizeof(int))
                    {
                        // Yes. Get the message length and read the remainder.
                        messageLength = BitConverter.ToInt32(messageStream.GetBuffer(), 0);

                        // Read up to the end of this message.
                        Parse(offset + sizeBytesToRead, bytes - sizeBytesToRead);
                    } // else we also don't have anything else anymore.
                }
            }

            #endregion

            #region Dispatching

            /// <summary>
            /// Dispatch a data event, meaning we received some data.
            /// </summary>
            private void OnData(EventArgs e)
            {
                if (Data != null)
                {
                    Data(this, e);
                }
            }

            /// <summary>
            /// Dispatch a disconnected event, meaning the connection died.
            /// </summary>
            private void OnDisconnected(EventArgs e)
            {
                if (Disconnected != null)
                {
                    Disconnected(this, e);
                }
            }

            #endregion
        }

        /// <summary>
        /// Utility event args to forward a received message.
        /// </summary>
        private sealed class ConnectionDataEventArgs : EventArgs
        {
            /// <summary>
            /// The data in the received message.
            /// </summary>
            public byte[] Data { get; private set; }

            /// <summary>
            /// The endpoint the message was received from.
            /// </summary>
            public IPEndPoint RemoteEndPoint { get; private set; }

            public ConnectionDataEventArgs(byte[] message, IPEndPoint endPoint)
            {
                this.Data = message;
                this.RemoteEndPoint = endPoint;
            }
        }

        #endregion
    }
}
