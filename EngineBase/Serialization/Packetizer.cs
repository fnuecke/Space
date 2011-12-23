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
        #region Fields
        
        /// <summary>
        /// Keep track of registered types.
        /// </summary>
        private static Dictionary<string, Func<IPacketizable>> _constructors = new Dictionary<string, Func<IPacketizable>>();

        #endregion

        #region Public API

        /// <summary>
        /// Register a new type for serializing / deserializing.
        /// </summary>
        /// <typeparam name="T">the type to register.</typeparam>
        public static void Register<T>() where T : IPacketizable, new()
        {
            string fullName = typeof(T).AssemblyQualifiedName;
            if (!_constructors.ContainsKey(fullName))
            {
                _constructors.Add(fullName, delegate() {
                    return (T)Activator.CreateInstance(typeof(T));
                });
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
        /// <returns>the specified packet with the value written to it.</returns>
        public static Packet Packetize<T>(T value, Packet packet) where T : IPacketizable
        {
            string fullName = value.GetType().AssemblyQualifiedName;
            if (_constructors.ContainsKey(fullName))
            {
                packet.Write(fullName);
                return packet.Write(value);
            }
            else
            {
                throw new ArgumentException(string.Format("Unregistered type: {0}", fullName), "T");
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
            if (_constructors.ContainsKey(fullName))
            {
                T result = (T)_constructors[fullName]();
                return packet.ReadPacketizable<T>(result);
            }
            else
            {
                throw new ArgumentException(string.Format("Unregistered type: {0}", fullName), "T");
            }
        }
        #endregion
    }
}
