using System;
using System.Net;
using System.Net.Sockets;
using Engine.Serialization;

namespace Engine.Network
{
    /// <summary>
    /// Event args used for simple protocol events.
    /// </summary>
    public class ProtocolEventArgs : EventArgs
    {
        /// <summary>
        /// The remote end point the event applies to.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }

        public ProtocolEventArgs(IPEndPoint endPoint)
        {
            this.RemoteEndPoint = endPoint;
        }
    }

    /// <summary>
    /// Event args used for protocol events including data.
    /// </summary>
    public class ProtocolDataEventArgs : ProtocolEventArgs
    {
        /// <summary>
        /// The data we received.
        /// </summary>
        public Packet Data { get; private set; }

        public ProtocolDataEventArgs(IPEndPoint endPoint, Packet data)
            : base(endPoint)
        {
            this.Data = data;
        }
    }

    /// <summary>
    /// Event args used for the TCP protocol's connected events.
    /// </summary>
    public class TcpProtocolConnectEventArgs : EventArgs
    {
        /// <summary>
        /// Whether the attempt to connect was successful or not.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// If the connection failed, this is the reason for it.
        /// </summary>
        public SocketException Exception { get; private set; }

        public TcpProtocolConnectEventArgs(bool success)
        {
            this.Success = success;
        }

        public TcpProtocolConnectEventArgs(bool success, SocketException exception)
            : this(success)
        {
            this.Exception = exception;
        }
    }
}
