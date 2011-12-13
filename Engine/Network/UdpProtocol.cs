using System;
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

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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
        private UdpClient udp;

        #endregion

        #region Constructor / Cleanup

        /// <summary>
        /// Creates a new UDP socket listening on the given port for the given protocol.
        /// </summary>
        /// <param name="port">the port to listen on/send through. If this is 0 we can only send.</param>
        /// <param name="protocolHeader">header of the used protocol (filter packages).</param>
        /// <param name="multicastGroup">the multicast group to join, if any.</param>
        public UdpProtocol(ushort port, byte[] protocolHeader, IPAddress multicastGroup = null)
            : base(protocolHeader)
        {
            // Create our actual udp socket.
            if (port > 0)
            {
                udp = new UdpClient(port);

                // Register as a local loopback.
                Loopback = new IPEndPoint(IPAddress.Loopback, port);
                loopbacksByPort[port] = this;
                boundPorts[port] = true;

                // Join multicast group to receive multicast messages.
                if (multicastGroup != null)
                {
                    udp.JoinMulticastGroup(multicastGroup);
                }
            }
            else
            {
                udp = new UdpClient();
            }
        }

        /// <summary>
        /// Close this connection for good. This class should not be used again after calling this.
        /// </summary>
        public override void Dispose()
        {
            if (Loopback != null)
            {
                ushort port = (ushort)((IPEndPoint)udp.Client.LocalEndPoint).Port;
                loopbacksByPort.Remove(port);
                boundPorts[port] = false;
            }

            udp.Close();

            GC.SuppressFinalize(this);
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
            if (udp.Client != null)
            {
                var remote = new IPEndPoint(0, 0);
                while (udp.Available > 0)
                {
                    try
                    {
                        byte[] message = udp.Receive(ref remote);
                        HandleReceive(message, remote);
                    }
                    catch (SocketException ex)
                    {
                        logger.TraceException("Socket connection died.", ex);
                    }
                }
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
            // Are we delivering locally?
            if (IPAddress.IsLoopback(endPoint.Address) &&
                loopbacksByPort.ContainsKey((ushort)endPoint.Port))
            {
                // Yes, inject directly.
                loopbacksByPort[(ushort)endPoint.Port].HandleReceive(message, Loopback);
            }
            else
            {
                // No, send via the network.
                udp.Send(message, message.Length, endPoint);
            }
        }

        #endregion

        #region LAN via Loopback Emulation

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

    }
}
