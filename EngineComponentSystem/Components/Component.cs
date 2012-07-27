using System;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Diagnostics;
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
    [DebuggerTypeProxy(typeof(FlattenHierarchyProxy))]
    public abstract class Component : IPacketizable, IHashable
    {
        #region Constants

        /// <summary>
        /// Reusable static instance of the comparer to be used for components.
        /// </summary>
        public static readonly ComponentComparer Comparer = new ComponentComparer();

        #endregion

        #region Type ID

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public abstract int GetTypeId();

        #endregion

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
            if (!other.GetType().TypeHandle.Equals(GetType().TypeHandle))
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
            hasher.Put(GetType().AssemblyQualifiedName);
            hasher.Put(Id);
            hasher.Put(Entity);
            hasher.Put(Enabled);
        }

        #endregion

        #region Object

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return GetType().Name + ": Id=" + Id + ", Enabled=" + Enabled;
        }

        #endregion

        #region Comparer

        /// <summary>
        /// Comparer implementation for components, to allow sorted inserting
        /// in the component list.
        /// </summary>
        public sealed class ComponentComparer : IComparer<Component>
        {
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <returns>
            /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table.Value Meaning Less than zero<paramref name="x"/> is less than <paramref name="y"/>.Zero<paramref name="x"/> equals <paramref name="y"/>.Greater than zero<paramref name="x"/> is greater than <paramref name="y"/>.
            /// </returns>
            /// <param name="x">The first object to compare.</param><param name="y">The second object to compare.</param>
            public int Compare(Component x, Component y)
            {
                return x.Id - y.Id;
            }
        }

        #endregion
    }
}
