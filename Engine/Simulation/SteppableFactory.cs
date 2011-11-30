using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Factory class used in states to produce steppable instances.
    /// This allows a central tracking of UIDs for steppables.
    /// </summary>
    public sealed class SteppableFactory<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : ISteppableFactory<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPacketizerContext>
    {
        #region Fields

        /// <summary>
        /// Counter used to distribute ids. Start with one to avoid accessing
        /// the first instance due to uninitialized "pointers".
        /// </summary>
        private long lastUid = 1;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new, fresh factory.
        /// </summary>
        public SteppableFactory()
        {
        }

        /// <summary>
        /// Used for cloning.
        /// </summary>
        private SteppableFactory(long lastUid)
        {
            this.lastUid = lastUid;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a unique ID for a steppable. Code pattern should look like this:
        /// <example>
        /// <code>
        /// var obj = factory.GetUniqueId(new Blah(...));
        /// </code>
        /// </example>
        /// </summary>
        /// <typeparam name="T">the type of steppable.</typeparam>
        /// <param name="value">the object for which to get a unique id.</param>
        /// <returns>the same steppable instance, now with a unique id.</returns>
        public T GetUniqueId<T>(T value) where T : TSteppable
        {
            value.UID = lastUid++;
            return value;
        }

        /// <summary>
        /// Simply increment the internal counter by one. This is useful if
        /// a state gets an existing object, which already has an id.
        /// </summary>
        public void Increment()
        {
            ++lastUid;
        }

        #endregion

        #region Serialization

        public object Clone()
        {
            return new SteppableFactory<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>(lastUid);
        }

        public void Packetize(Packet packet)
        {
            packet.Write(lastUid);
        }

        public void Depacketize(Packet packet, TPacketizerContext context)
        {
            lastUid = packet.ReadInt64();
        }

        #endregion

    }
}
