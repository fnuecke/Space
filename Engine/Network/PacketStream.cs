using System;
using System.IO;
using System.Net.Sockets;
using Engine.Serialization;

namespace Engine.Network
{
    /// <summary>
    /// Utility class for sending packets over a network stream.
    /// </summary>
    public class PacketStream
    {
        #region Properties

        /// <summary>
        /// Number of bytes available from our buffer.
        /// </summary>
        private int Available { get { return bufferDataLength - bufferReadPosition; } }

        #endregion

        #region Fields

        /// <summary>
        /// The underlying network stream being used.
        /// </summary>
        private NetworkStream stream;

        /// <summary>
        /// Buffer for reading from the stream.
        /// </summary>
        private byte[] buffer = new byte[512];

        /// <summary>
        /// The actual number of valid bytes in our buffer.
        /// </summary>
        private int bufferDataLength = 0;

        /// <summary>
        /// The position in our buffer we're currently reading from.
        /// </summary>
        private int bufferReadPosition = 0;

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

        public PacketStream(NetworkStream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            messageStream.Dispose();
            stream.Dispose();
        }

        #endregion

        #region Read / Write

        /// <summary>
        /// Tries reading a single packet from this stream. To read multiple available
        /// packets, repeatedly call this method until it returns <c>null</c>.
        /// </summary>
        /// <returns>A read packet, if one was available.</returns>
        /// <exception cref="SocketException">If the underlying stream fails.</exception>
        public override Packet Read()
        {
            // Read until we either find a complete packet, or cannot read any more data.
            while (Available > 0 || stream.DataAvailable)
            {
                // Parse what's left in our buffer. If we find a packet, we return it.
                Packet packet = Parse();
                if (packet != null)
                {
                    return packet;
                }
                // Else we're guaranteed to be at the end of our buffer, and it wasn't
                // enough, so we get some more.
                if (stream.DataAvailable)
                {
                    // Get what we can fit in our buffer.
                    bufferDataLength = stream.Read(buffer, 0, buffer.Length);
                    if (bufferDataLength <= 0)
                    {
                        // Connection died (reading 0 bytes means the connection is gone).
                        throw new SocketException();
                    }
                    // Reset our read position.
                    bufferReadPosition = 0;
                }
            }
            // Got here means we failed.
            return null;
        }

        /// <summary>
        /// Writes the specified packet to the underlying stream. It can then be read
        /// byte a <c>PacketStream</c> on the other end of the stream.
        /// </summary>
        /// <param name="packet">The packet to write.</param>
        public override void Write(Packet packet)
        {
            if (packet.Length > 0)
            {
                stream.Write(BitConverter.GetBytes(packet.Length), 0, sizeof(int));
                stream.Write(packet.Buffer, 0, packet.Length);
            }
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Try to parse a message from the data that's currently in the buffer.
        /// </summary>
        private Packet Parse()
        {
            if (messageLength <= 0)
            {
                // Message size unknown. Figure out how much more we need to read, and
                // read at most that much.
                int remainingSizeBytes = sizeof(int) - (int)messageStream.Position;
                int sizeBytesToRead = System.Math.Min(Available, remainingSizeBytes);
                messageStream.Write(buffer, bufferReadPosition, sizeBytesToRead);
                bufferReadPosition += sizeBytesToRead;

                // Do we have enough data to figure out the size now?
                if (messageStream.Position == sizeof(int))
                {
                    // Yes. Get the message length and read the remainder.
                    messageLength = BitConverter.ToInt32(messageStream.GetBuffer(), 0);
                    if (messageLength < 0)
                    {
                        // Invalid data, consider this connection broken.
                        throw new SocketException();
                    }
                    // Reset so the rest is just the data.
                    messageStream.SetLength(0);
                    messageStream.Position = 0;

                    // Read up to the end of this message.
                    return Parse();
                } // else we also don't have anything else anymore.
            }
            else
            {
                // We already know our current message size. See if we can complete the
                // message.
                int remainingBodyBytes = messageLength - (int)messageStream.Position;
                int bodyBytesToRead = System.Math.Min(Available, remainingBodyBytes);
                messageStream.Write(buffer, bufferReadPosition, bodyBytesToRead);
                bufferReadPosition += bodyBytesToRead;

                // We done yet?
                if (messageStream.Position == messageLength)
                {
                    // Yep. Wrap up a packet, reset and return it.
                    Packet packet = new Packet(messageStream.ToArray());
                    // Reset for the next message.
                    messageStream.SetLength(0);
                    messageStream.Position = 0;
                    return packet;
                }
            }
            // Could not complete a packet.
            return null;
        }

        #endregion
    }
}
