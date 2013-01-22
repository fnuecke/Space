using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Engine.ComponentSystem.Components;
using Engine.Diagnostics;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>Base class for systems, implementing default basic functionality.</summary>
    [DebuggerTypeProxy(typeof (FlattenHierarchyProxy))]
    public abstract class AbstractSystem : ICopyable<AbstractSystem>, IPacketizable
    {
        #region Type ID

        /// <summary>
        ///     Gets the component type id for the calling currently-being-initialized component type class. This will create
        ///     a new ID if necessary.
        /// </summary>
        /// <returns>The type id for that component.</returns>
        /// <remarks>
        ///     Utility method for subclasses, this just redirects to the same method in the component system manager. Uses
        ///     execution stack to determine calling type.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected static int CreateTypeId()
        {
            return ComponentSystem.Manager.GetSystemTypeId(new StackFrame(1, false).GetMethod().DeclaringType);
        }

        #endregion

        #region Properties

        /// <summary>The component system manager this system is part of.</summary>
        [CopyIgnore, PacketizerIgnore]
        public IManager Manager { get; set; }

        #endregion

        #region Manager Events

        /// <summary>Called by the manager when an entity was removed.</summary>
        /// <param name="entity">The entity that was removed.</param>
        public virtual void OnEntityRemoved(int entity) {}

        /// <summary>Called by the manager when a new component was added.</summary>
        /// <param name="component">The component that was added.</param>
        public virtual void OnComponentAdded(IComponent component) {}

        /// <summary>Called by the manager when a component was removed.</summary>
        /// <param name="component">The component that was removed.</param>
        public virtual void OnComponentRemoved(IComponent component) {}

        /// <summary>
        ///     Called by the manager when the complete environment has been depacketized. Called from the <see cref="Manager"/>.
        /// </summary>
        public virtual void OnDepacketized() {}

        /// <summary>
        ///     Called by the manager when the complete environment has been copied from another manager. Called from the
        ///     <see cref="Manager"/>.
        /// </summary>
        public virtual void OnCopied() {}

        /// <summary>
        ///     Called by the manager when the system was added to it. This allows for the system to register its message
        ///     listener and do other one-time initialization.
        /// </summary>
        public virtual void OnAddedToManager() {}

        #endregion

        #region Serialization / Hashing

        /// <summary>Write the object's state to the given packet.</summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        [OnPacketize]
        public virtual IWritablePacket Packetize(IWritablePacket packet)
        {
            return packet;
        }

        /// <summary>
        ///     Bring the object to the state in the given packet. This is called before automatic depacketization is
        ///     performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        [OnPostDepacketize]
        public virtual void Depacketize(IReadablePacket packet) {}

        [OnStringify]
        public virtual StreamWriter Dump(StreamWriter w, int indent)
        {
            return w;
        }

        #endregion

        #region Copying

        /// <summary>Creates a new copy of the object, that shares no mutable references with this instance.</summary>
        /// <returns>The copy.</returns>
        public virtual AbstractSystem NewInstance()
        {
            // Not supported for presentation types.
            if (this is IDrawingSystem)
            {
                throw new InvalidOperationException("Drawing systems cannot be copied.");
            }

            var copy = (AbstractSystem) MemberwiseClone();

            copy.Manager = null;

            return copy;
        }

        /// <summary>
        ///     Creates a deep copy of the system. The passed system must be of the same type.
        ///     <para>
        ///         This clones any contained data types to return an instance that represents a complete copy of the one passed
        ///         in.
        ///     </para>
        /// </summary>
        /// <param name="into">The instance to copy into.</param>
        /// <remarks>The manager for the system to copy into must be set to the manager into which the system is being copied.</remarks>
        public virtual void CopyInto(AbstractSystem into)
        {
            // Not supported for presentation types.
            if (this is IDrawingSystem)
            {
                throw new InvalidOperationException("Drawing systems cannot be copied.");
            }

            // Don't allow identity copying.
            // TODO might relax this to simply returning, but this normally indicates unwanted behavior.
            if (into == this)
            {
                throw new ArgumentException("Cannot copy into self.", "into");
            }

            // Manager must be re-set to new owner before copying.
            if (into.Manager == null)
            {
                throw new ArgumentException("Target must have a Manager.", "into");
            }

            // Systems should never have to be copied inside the same context.
            if (into.Manager == Manager)
            {
                throw new ArgumentException("Target must have a different Manager.", "into");
            }

            // Use dynamic function to do basic copying.
            Copyable.CopyInto(this, into);
        }

        #endregion
    }
}