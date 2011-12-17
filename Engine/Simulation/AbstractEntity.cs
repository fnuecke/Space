using Engine.Serialization;
using Engine.Util;

namespace Engine.Simulation
{
    /// <summary>
    /// Base class for entities, implementing logic for distributing unique ids.
    /// </summary>
    public abstract class AbstractEntity<TPlayerData> : IEntity<TPlayerData>
        where TPlayerData : IPacketizable<TPlayerData>
    {
        #region Properties

        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        public long UID { get; set; }

        /// <summary>
        /// The world (simulation) this object is associated with.
        /// </summary>
        public virtual IState<TPlayerData> State { get; set; }

        #endregion

        protected AbstractEntity()
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
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public abstract void Hash(Hasher hasher);

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
        public virtual void Depacketize(Packet packet, IPacketizerContext<TPlayerData> context)
        {
            UID = packet.ReadInt64();
        }

        #endregion
    }
}
