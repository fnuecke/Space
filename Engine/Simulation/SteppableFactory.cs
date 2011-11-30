using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Factory class used in states to produce steppable instances.
    /// This allows a central tracking of UIDs for steppables.
    /// </summary>
    public sealed class SteppableFactory<TState, TSteppable, TCommandType, TPlayerData> : ISteppableFactory<TState, TSteppable, TCommandType, TPlayerData>
        where TState : IState<TState, TSteppable, TCommandType, TPlayerData>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData>
        where TCommandType : struct
        where TPlayerData : IPacketizable
    {
        #region Fields

        /// <summary>
        /// Counter used to distribute ids.
        /// </summary>
        private long lastUid = 0;

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

        #endregion

        #region Serialization

        public object Clone()
        {
            return new SteppableFactory<TState, TSteppable, TCommandType, TPlayerData>(lastUid);
        }

        public void Packetize(Packet packet)
        {
            packet.Write(lastUid);
        }

        public void Depacketize(Packet packet)
        {
            lastUid = packet.ReadInt64();
        }

        #endregion

    }
}
