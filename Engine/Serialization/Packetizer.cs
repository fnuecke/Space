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
    public sealed class Packetizer<TPlayerData> : IPacketizer<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
    {
        #region Properties
        
        /// <summary>
        /// The context used by depacketize methods.
        /// </summary>
        public IPacketizerContext<TPlayerData> Context { get; private set; }

        #endregion

        #region Fields
        
        /// <summary>
        /// Keep track of registered types.
        /// </summary>
        private Dictionary<string, Func<IPacketizable<TPlayerData>>> _constructors = new Dictionary<string, Func<IPacketizable<TPlayerData>>>();

        #endregion

        #region Constructor
        
        public Packetizer(IPacketizerContext<TPlayerData> context)
        {
            this.Context = context;
        }

        // For copying.
        private Packetizer()
        {
        }

        #endregion

        #region Public API

        /// <summary>
        /// Register a new type for serializing / deserializing.
        /// </summary>
        /// <typeparam name="T">the type to register.</typeparam>
        public void Register<T>() where T : IPacketizable<TPlayerData>, new()
        {
            Type type = typeof(T);
            if (!_constructors.ContainsKey(type.FullName))
            {
                _constructors.Add(type.FullName, delegate() { return (T)typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]); });
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
        public void Packetize<T>(T value, Packet packet) where T : IPacketizable<TPlayerData>
        {
            Type type = value.GetType();
            if (_constructors.ContainsKey(type.FullName))
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
        public T Depacketize<T>(Packet packet) where T : IPacketizable<TPlayerData>
        {
            string fullName = packet.ReadString();
            if (_constructors.ContainsKey(fullName))
            {
                IPacketizable<TPlayerData> result = _constructors[fullName]();
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
        public IPacketizer<TPlayerData> CopyFor(ISession<TPlayerData> session)
        {
            var copy = new Packetizer<TPlayerData>();
            copy.Context = (IPacketizerContext<TPlayerData>)this.Context.Clone();
            copy.Context.Session = session;
            copy._constructors = this._constructors;
            return copy;
        }

        #endregion
    }
}
