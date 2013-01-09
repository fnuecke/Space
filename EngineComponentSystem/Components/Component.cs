using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Engine.Diagnostics;
using Engine.Serialization;

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
    public abstract class Component : IPacketizable
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

        /// <summary>
        /// Gets the component type id for the calling currently-being-initialized
        /// component type class. This will create a new ID if necessary.
        /// </summary>
        /// <returns>The type id for that component.</returns>
        /// <remarks>
        /// Utility method for subclasses, this just redirects to the same method in
        /// the component system manager. Uses execution stack to determine calling
        /// type.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected static int CreateTypeId()
        {
            return ComponentSystem.Manager.GetComponentTypeId(new StackFrame(1, false).GetMethod().DeclaringType);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The manager the component lives in.
        /// </summary>
        [PacketizerIgnore]
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
        public virtual bool Enabled { get; set; }

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
        [OnPacketize]
        public virtual IWritablePacket Packetize(IWritablePacket packet)
        {
            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// after automatic depacketization has been performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        [OnPostDepacketize]
        public virtual void PostDepacketize(IReadablePacket packet)
        {
        }

        /// <summary>
        /// Writes a string representation of the object to a string builder.
        /// </summary>
        /// <param name="w"> </param>
        /// <param name="indent">The indentation level.</param>
        /// <returns>The string builder, for call chaining.</returns>
        [OnStringify]
        public virtual StreamWriter Dump(StreamWriter w, int indent)
        {
            return w;
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
