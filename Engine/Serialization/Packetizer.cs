using System;
using System.Collections.Generic;

namespace Engine.Serialization
{
    /// <summary>
    /// Utility class for also serializing type information. This allows
    /// serialization / deserialization if the code triggering the deserialization
    /// does not know the actual type to expect in the data.
    /// </summary>
    public static class Packetizer
    {
        /// <summary>
        /// Keep track of registered types.
        /// </summary>
        private static Dictionary<string, Func<IPacketizable>> constructors = new Dictionary<string, Func<IPacketizable>>();

        /// <summary>
        /// Register a new type for serializing / deserializing.
        /// </summary>
        /// <typeparam name="T">the type to register.</typeparam>
        public static void Register<T>() where T : IPacketizable, new()
        {
            Type type = typeof(T);
            if (!constructors.ContainsKey(type.FullName))
            {
                constructors.Add(type.FullName, delegate() { return (T)typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]); });
            }
        }

        /// <summary>
        /// Write an object to a packet, including type information for deserialization
        /// under the condition that the actual expected type is unknown.
        /// </summary>
        /// <typeparam name="T">the actual type of the object to write.</typeparam>
        /// <param name="value">the object to write.</param>
        /// <param name="packet">the packet to write to.</param>
        /// <exception cref="ArgumentException">if the type has not been registered beforehand.</exception>
        public static void Packetize<T>(T value, Packet packet) where T : IPacketizable
        {
            Type type = value.GetType();
            if (constructors.ContainsKey(type.FullName))
            {
                packet.Write(type.FullName);
                value.Packetize(packet);
            }
            else
            {
                throw new ArgumentException("Unknown type, register it first.");
            }
        }

        /// <summary>
        /// Parse an object of an unknown type at a certain state from a packet.
        /// </summary>
        /// <typeparam name="T">a known supertype of the object.</typeparam>
        /// <param name="packet">the packet to read from.</param>
        /// <returns>the deserialized object.</returns>
        /// <exception cref="ArgumentException">if the type has not been registered beforehand.</exception>
        public static T Depacketize<T>(Packet packet) where T : IPacketizable
        {
            string fullName = packet.ReadString();
            if (constructors.ContainsKey(fullName))
            {
                IPacketizable result = constructors[fullName]();
                result.Depacketize(packet);
                return (T)result;
            }
            else
            {
                throw new ArgumentException("Unknown type, register");
            }
        }
    }
}
