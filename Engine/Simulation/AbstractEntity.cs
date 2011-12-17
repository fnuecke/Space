using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;
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
        /// A list of all of this entities components.
        /// </summary>
        public ReadOnlyCollection<IComponent<TPlayerData>> Components { get { return components.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// List of all this entities components.
        /// </summary>
        protected List<IComponent<TPlayerData>> components = new List<IComponent<TPlayerData>>();

        #endregion

        protected AbstractEntity()
        {
            // Init to -1 as a default, so these aren't found due to
            // badly initialized 'pointers'.
            this.UID = -1;
        }

        #region Interfaces

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
