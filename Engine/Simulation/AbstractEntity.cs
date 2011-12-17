﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Engine.ComponentSystem.Components;

namespace Engine.Simulation
{
    /// <summary>
    /// Base class for entities, implementing logic for distributing unique ids.
    /// </summary>
    public abstract class AbstractEntity : IEntity
    {
        #region Properties

        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        public long UID { get; set; }

        /// <summary>
        /// A list of all of this entities components.
        /// </summary>
        public ReadOnlyCollection<IComponent> Components { get { return components.AsReadOnly(); } }

        #endregion

        #region Fields

        /// <summary>
        /// List of all this entities components.
        /// </summary>
        protected List<IComponent> components = new List<IComponent>();

        #endregion

        protected AbstractEntity()
        {
            // Init to -1 as a default, so these aren't found due to
            // badly initialized 'pointers'.
            this.UID = -1;
        }

        #region Interfaces

        /// <summary>
        /// Create a (deep!) copy of the object.
        /// </summary>
        /// <returns></returns>
        public abstract object Clone();

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">the packet to write the data to.</param>
        public virtual void Packetize(Serialization.Packet packet)
        {
            packet.Write(UID);
            foreach (var component in components)
            {
                component.Packetize(packet);
            }
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">the packet to read from.</param>
        public virtual void Depacketize(Serialization.Packet packet, Serialization.IPacketizerContext context)
        {
            UID = packet.ReadInt64();
            foreach (var component in components)
            {
                component.Depacketize(packet, context);
            }
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">the hasher to push data to.</param>
        public virtual void Hash(Util.Hasher hasher)
        {
        }

        #endregion
    }
}
