using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Engine.Network
{
    /// <summary>
    /// Base class for UDP based communication.
    /// </summary>
    public sealed class UdpProtocol : AbstractProtocol
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Properties

        /// <summary>
        /// The local loopback address for this protocol. Use this to send
        /// messages to from another protocol.
        /// </summary>
        public IPEndPoint Loopback { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// The actual underlying UDP socket.
        /// </summary>
        private UdpClient _udp;

        #endregion

        #region Constructor / Cleanup
        
        /// <summary>
        /// Creates a new UDP socket listening on the given port for the given protocol.
        /// </summary>
        /// <param name="endPoint">The local endpoint to bind to, with multicast enabled.</param>
        /// <param name="protocolHeader">header of the used protocol (filter packages).</param>
        public UdpProtocol(IPEndPoint endPoint, byte[] protocolHeader)
            : base(protocolHeader)
        {
            _udp = new UdpClient(endPoint.Port);

            // Register as a local loopback.
            Loopback = new IPEndPoint(IPAddress.Loopback, endPoint.Port);
            LoopbacksByPort[endPoint.Port] = this;
            BoundPorts[endPoint.Port] = true;

            // Join multicast group to receive multicast messages.
            _udp.JoinMulticastGroup(endPoint.Address);
        }

        /// <summary>
        /// Creates a new UDP socket for sending only.
        /// </summary>
        /// <param name="protocolHeader">header of the used protocol (filter packages).</param>
        public UdpProtocol(byte[] protocolHeader)
            : base(protocolHeader)
        {
            _udp = new UdpClient();
        }

        /// <summary>
        /// Close this connection for good. This class should not be used again after calling this.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _udp != null)
            {
                if (Loopback != null)
                {
                    var port = (ushort)((IPEndPoint)_udp.Client.LocalEndPoint).Port;
                    LoopbacksByPort.Remove(port);
                    BoundPorts[port] = false;
                }

                _udp.Close();
                _udp = null;
            }
        }

        #endregion

        #region Send / Receive

        /// <summary>
        /// Try receiving some packets. This will trigger the <c>Data</c> event
        /// for each received packet.
        /// </summary>
        /// <remarks>implementations in subclasses shoudl call <c>HandleReceive</c>
        /// with the read raw data.</remarks>
        public override void Receive()
        {
            // TODO no idea why this happens after cleanup...
            if (_udp != null)
            {
                var remote = new IPEndPoint(0, 0);
                while (_udp.Available > 0)
                {
                    try
                    {
                        byte[] message = _udp.Receive(ref remote);
                        HandleReceive(message, remote);
                    }
                    catch (SocketException ex)
                    {
                        Logger.TraceException("Socket connection died.", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Use this to implement actual sending of the given data to the given endpoint.
        /// </summary>
        /// <param name="message">the data to send/</param>
        /// <param name="endPoint">the end point to send it to.</param>
        protected override void HandleSend(byte[] message, IPEndPoint endPoint)
        {
            // Are we delivering locally?
            if (IPAddress.IsLoopback(endPoint.Address) &&
                LoopbacksByPort.ContainsKey((ushort)endPoint.Port))
            {
                // Yes, inject directly.
                LoopbacksByPort[(ushort)endPoint.Port].HandleReceive(message, Loopback);
            }
            else
            {
                // No, send via the network.
                _udp.Send(message, message.Length, endPoint);
            }
        }

        #endregion

        #region LAN via Loopback Emulation

        /// <summary>
        /// This is used to determine the local loopback address to use for a new
        /// instance.
        /// </summary>
        private static readonly BitArray BoundPorts = new BitArray(ushort.MaxValue);

        /// <summary>
        /// Lookup table the instances add themselves to. This essentially fakes
        /// a tiny LAN which only exists inside the running program.
        /// </summary>
        private static readonly Dictionary<int, UdpProtocol> LoopbacksByPort = new Dictionary<int, UdpProtocol>();

        #endregion

    }
}
