﻿using System;
using System.Net;
using Engine.Serialization;
using Engine.Util;

namespace Engine.Network
{
    public abstract class AbstractProtocol : IDisposable
    {
        #region Events

        /// <summary>
        /// Register here to be notified of incoming data packets.
        /// </summary>
        public event EventHandler<EventArgs> Data;

        #endregion

        #region Fields

        /// <summary>
        /// The header we use for all messages.
        /// </summary>
        private byte[] _header;

        private bool _disposed;

        #endregion
        
        #region Constructor / Cleanup

        /// <summary>
        /// Initializes base stuff for protocol.
        /// </summary>
        /// <param name="protocolHeader">header of the used protocol (filter packages).</param>
        protected AbstractProtocol(byte[] protocolHeader)
        {
            // Remember our header.
            _header = protocolHeader;
        }

        /// <summary>
        /// Close this connection for good. This class should not be used again after calling this.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        #endregion

        #region Types

        /// <summary>
        /// Possible message flags.
        /// </summary>
        private enum MessageFlags
        {
            /// <summary>
            /// No flags set.
            /// </summary>
            None = 0,

            /// <summary>
            /// The message is marked to be compressed.
            /// </summary>
            Compressed = 1
        }

        #endregion

        #region Constants

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

        #endregion

        #region Send / Receive

        /// <summary>
        /// Send a packet to a remote host.
        /// </summary>
        /// <param name="packet">the data to send</param>
        /// <param name="endPoint">the remote end point to send it to.</param>
        public void Send(Packet packet, IPEndPoint endPoint)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }
            if (packet.Length < 1)
            {
                throw new ArgumentException("packet");
            }
            if (endPoint == null)
            {
                throw new ArgumentNullException("endPoint");
            }

            HandleSend(MakeMessage(packet), endPoint);
        }

        /// <summary>
        /// Try receiving some packets. This will trigger the <c>Data</c> event
        /// for each received packet.
        /// </summary>
        /// <remarks>implementations in subclasses shoudl call <c>HandleReceive</c>
        /// with the read raw data.</remarks>
        public abstract void Receive();

        /// <summary>
        /// Use this to implement actual sending of the given data to the given endpoint.
        /// </summary>
        /// <param name="message">the data to send/</param>
        /// <param name="bytes">the length of the data to send.</param>
        /// <param name="endPoint">the end point to send it to.</param>
        protected abstract void HandleSend(byte[] message, IPEndPoint endPoint);

        #endregion

        #region Utility methods

        /// <summary>
        /// Call this to handle a received message.
        /// </summary>
        /// <param name="buffer">the data to inject.</param>
        /// <param name="remote">the remote host the message was received from.</param>
        /// <returns>whether the message was parsed successfully.</returns>
        protected bool HandleReceive(byte[] buffer, IPEndPoint endPoint)
        {
            // Can we parse it?
            Packet message;
            if ((message = ParseMessage(buffer)) != null)
            {
                // Yes, so let's pass it on.
                OnData(new ProtocolDataEventArgs(endPoint, message));
                return true;
            }
            else
            {
                // Invalid data received.
                return false;
            }
        }

        /// <summary>
        /// Parse received data to check if it's a message we can handle, and parse it.
        /// </summary>
        /// <param name="message">the data to parse.</param>
        /// <returns>the data parsed from the message, or <c>null</c> on failure.</returns>
        private Packet ParseMessage(byte[] message)
        {
            // Check the header.
            if (IsHeaderValid(message))
            {
                // OK, get the decrypted packet.
                var packet = new Packet(crypto.Decrypt(message, _header.Length, message.Length - _header.Length));

                // Get message flags.
                if (packet.HasByte())
                {
                    MessageFlags flags = (MessageFlags)packet.ReadByte();

                    // Get the message body.
                    if (packet.HasByteArray())
                    {
                        if ((flags & MessageFlags.Compressed) > 0)
                        {
                            return new Packet(SimpleCompression.Decompress(packet.ReadByteArray()));
                        }
                        else
                        {
                            return new Packet(packet.ReadByteArray());
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Create a new message, compressing it if it makes the message smaller,
        /// and encrypting it. Finally, we put the header in front of it.
        /// </summary>
        /// <param name="packet">the packet to make a message of.</param>
        /// <returns>the message data.</returns>
        private byte[] MakeMessage(Packet packet)
        {
            byte[] data = null;
            int length = 0;
            MessageFlags flags = MessageFlags.None;
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
                        flags |= MessageFlags.Compressed;
                        data = compressed;
                        length = compressed.Length;
                    }
                }
            }
            Packet withFlags = new Packet(1 + sizeof(ushort) + length);
            withFlags.Write((byte)flags);
            withFlags.Write(data);

            byte[] encrypted = crypto.Encrypt(withFlags.Buffer, 0, withFlags.Length);
            byte[] withHeader = new byte[_header.Length + encrypted.Length];
            _header.CopyTo(withHeader, 0);
            encrypted.CopyTo(withHeader, _header.Length);
            return withHeader;
        }

        /// <summary>
        /// Utility method to check if a message header matches our own.
        /// </summary>
        /// <param name="buffer">the header to check.</param>
        /// <returns>whether it matches or not.</returns>
        private bool IsHeaderValid(byte[] buffer)
        {
            if (buffer.Length < _header.Length)
            {
                return false;
            }
            for (var i = 0; i < _header.Length; ++i)
            {
                if (buffer[i] != _header[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Helper to fire data events.
        /// </summary>
        private void OnData(ProtocolDataEventArgs e)
        {
            if (Data != null)
            {
                Data(this, e);
            }
        }

        #endregion
    }
}