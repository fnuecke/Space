using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Base class for steppables, implementing logic for distributing unique ids.
    /// </summary>
    /// <typeparam name="TState">the type of state the object will be used together with.</typeparam>
    /// <typeparam name="TSteppable">the type of steppable used in the state.</typeparam>
    public abstract class AbstractSteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TState : IState<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        public long UID { get; set; }

        /// <summary>
        /// The world (simulation) this object is associated with.
        /// </summary>
        public virtual TState State { get; set; }

        #endregion

        protected AbstractSteppable()
        {
            // Init to -1 as a default, so these aren't found due to
            // badly initialized 'pointers'.
            this.UID = -1;
        }

        #region Interfaces

        /// <summary>
        /// Perform one simulation step. 
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Create a (deep!) copy of the object.
        /// </summary>
        /// <returns></returns>
        public abstract object Clone();

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">the packet to write the data to.</param>
        public virtual void Packetize(Packet packet)
        {
            packet.Write(UID);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">the packet to read from.</param>
        public virtual void Depacketize(Packet packet, TPacketizerContext context)
        {
            UID = packet.ReadInt64();
        }

        #endregion
    }
}
