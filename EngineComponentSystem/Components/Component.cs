using System;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Utility base class for components adding default behavior.
    /// 
    /// <para>
    /// Implementing classes must take note while cloning: they must not
    /// hold references to other components, or if they do (caching) they
    /// must invalidate these references when cloning.
    /// </para>
    /// </summary>
    public abstract class Component : IPacketizable, IHashable
    {
        #region Properties

        /// <summary>
        /// The manager the component lives in.
        /// </summary>
        public IManager Manager { get; internal set; }

        /// <summary>
        /// Unique ID in the context of its entity. This means there can be
        /// multiple components with the same id, but no two components with
        /// the same id attached to the same entity.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Gets the entity this component belongs to.
        /// </summary>
        public int Entity { get; internal set; }

        #endregion

        #region Fields

        /// <summary>
        /// Whether the component is enabled or not. Disabled components will
        /// not be handled in the component's system's <c>Update()</c> method.
        /// </summary>
        public bool Enabled;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public virtual Component Initialize(Component other)
        {
            // Check if the component is of the correct type.
            if (other.GetType() != GetType())
            {
                throw new ArgumentException("Invalid type.", "other");
            }

            Enabled = other.Enabled;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public virtual void Reset()
        {
            Manager = null;
            Id = 0;
            Entity = 0;
            Enabled = false;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public virtual Packet Packetize(Packet packet)
        {
            return packet.Write(Id)
                .Write(Entity)
                .Write(Enabled);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            Manager = null;
            Id = packet.ReadInt32();
            Entity = packet.ReadInt32();
            Enabled = packet.ReadBoolean();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public virtual void Hash(Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(Id));
            hasher.Put(BitConverter.GetBytes(Entity));
            hasher.Put(BitConverter.GetBytes(Enabled));
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return GetType().Name + ": Id = " + Id + ", Enabled = " + Enabled;
        }

        #endregion
    }
}
