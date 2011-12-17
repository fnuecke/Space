using System;
using Engine.Session;

namespace Engine.Serialization
{
    /// <summary>
    /// Interface for the <c>Packetizer</c> class, used for serializing / deserializing objects.
    /// </summary>
    /// <typeparam name="TPacketizerContext"></typeparam>
    public interface IPacketizer<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
    {
        /// <summary>
        /// The context used by depacketize methods.
        /// </summary>
        IPacketizerContext<TPlayerData> Context { get; }

        /// <summary>
        /// Write an object to a packet, including type information for deserialization
        /// under the condition that the actual expected type is unknown.
        /// </summary>
        /// <typeparam name="T">the actual type of the object to write.</typeparam>
        /// <param name="value">the object to write.</param>
        /// <param name="packet">the packet to write to.</param>
        /// <exception cref="ArgumentException">if the type has not been registered beforehand.</exception>
        void Packetize<T>(T value, Packet packet) where T : IPacketizable<TPlayerData>;

        /// <summary>
        /// Parse an object of an unknown type at a certain state from a packet.
        /// </summary>
        /// <typeparam name="T">a known supertype of the object.</typeparam>
        /// <param name="packet">the packet to read from.</param>
        /// <returns>the deserialized object.</returns>
        /// <exception cref="ArgumentException">if the type has not been registered beforehand.</exception>
        T Depacketize<T>(Packet packet) where T : IPacketizable<TPlayerData>;

        /// <summary>
        /// Creates a clone of this packetizer, but with it's context set for the given session.
        /// </summary>
        /// <param name="session">the session for which to copy the packetizer.</param>
        /// <returns>a new packetizer for the given session.</returns>
        IPacketizer<TPlayerData> CopyFor(ISession<TPlayerData> session);
    }
}
