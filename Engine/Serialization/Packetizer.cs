using System;
using System.Collections.Generic;
using Engine.Session;

namespace Engine.Serialization
{
    /// <summary>
    /// Utility class for also serializing type information. This allows
    /// serialization / deserialization if the code triggering the deserialization
    /// does not know the actual type to expect in the data.
    /// </summary>
    public sealed class Packetizer<TPlayerData, TPacketizerContext> : IPacketizer<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// The context used by depacketize methods.
        /// </summary>
        public TPacketizerContext Context { get; private set; }

        /// <summary>
        /// Keep track of registered types.
        /// </summary>
        private Dictionary<string, Func<IPacketizable<TPlayerData, TPacketizerContext>>> constructors = new Dictionary<string, Func<IPacketizable<TPlayerData, TPacketizerContext>>>();

        public Packetizer(TPacketizerContext context)
        {
            this.Context = context;
        }

        // For copying.
        private Packetizer()
        {
        }

        /// <summary>
        /// Register a new type for serializing / deserializing.
        /// </summary>
        /// <typeparam name="T">the type to register.</typeparam>
        public void Register<T>() where T : IPacketizable<TPlayerData, TPacketizerContext>, new()
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
        public void Packetize<T>(T value, Packet packet) where T : IPacketizable<TPlayerData, TPacketizerContext>
        {
            Type type = value.GetType();
            if (constructors.ContainsKey(type.FullName))
            {
                packet.Write(type.FullName);
                value.Packetize(packet);
            }
            else
            {
                throw new ArgumentException("T");
            }
        }

        /// <summary>
        /// Parse an object of an unknown type at a certain state from a packet.
        /// </summary>
        /// <typeparam name="T">a known supertype of the object.</typeparam>
        /// <param name="packet">the packet to read from.</param>
        /// <returns>the deserialized object.</returns>
        /// <exception cref="ArgumentException">if the type has not been registered beforehand.</exception>
        public T Depacketize<T>(Packet packet) where T : IPacketizable<TPlayerData, TPacketizerContext>
        {
            string fullName = packet.ReadString();
            if (constructors.ContainsKey(fullName))
            {
                IPacketizable<TPlayerData, TPacketizerContext> result = constructors[fullName]();
                result.Depacketize(packet, Context);
                return (T)result;
            }
            else
            {
                throw new ArgumentException("T");
            }
        }

        /// <summary>
        /// Creates a clone of this packetizer, but with it's context set for the given session.
        /// </summary>
        /// <param name="session">the session for which to copy the packetizer.</param>
        /// <returns>a new packetizer for the given session.</returns>
        public IPacketizer<TPlayerData, TPacketizerContext> CopyFor(ISession<TPlayerData, TPacketizerContext> session)
        {
            var copy = new Packetizer<TPlayerData, TPacketizerContext>();
            copy.Context = (TPacketizerContext)this.Context.Clone();
            copy.Context.Session = session;
            copy.constructors = this.constructors;
            return copy;
        }
    }
}
