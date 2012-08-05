using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// Base class for components that implement collision logic.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public abstract class Collidable : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// This bit mask representing the collision groups this component is
        /// part of. Components sharing at least one group will not be tested
        /// against each other.
        /// </summary>
        public uint CollisionGroups;

        /// <summary>
        /// Previous position of the underlying physics component (for sweep tests).
        /// </summary>
        public FarPosition PreviousPosition;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherCollidable = (Collidable)other;
            CollisionGroups = otherCollidable.CollisionGroups;
            PreviousPosition = otherCollidable.PreviousPosition;

            return this;
        }

        /// <summary>
        /// Initialize the component with specified collision groups.
        /// </summary>
        /// <param name="groups">The groups.</param>
        protected Collidable Initialize(uint groups)
        {
            CollisionGroups = groups;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            CollisionGroups = 0;
            PreviousPosition = FarPosition.Zero;
        }

        #endregion

        #region Intersection

        /// <summary>
        /// Computes the current minimal bounding box for this collidable.
        /// </summary>
        /// <returns>The minimal boundsing box for this object.</returns>
        public abstract FarRectangle ComputeBounds();

        /// <summary>
        /// Test if this collidable collides with the specified one.
        /// </summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <returns>Whether the two collide or not.</returns>
        public abstract bool Intersects(Collidable collidable);

        internal abstract bool Intersects(ref Vector2 extents, ref FarPosition previousPosition, ref FarPosition position);

        internal abstract bool Intersects(float radius, ref FarPosition previousPosition, ref FarPosition position);

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(CollisionGroups)
                .Write(PreviousPosition);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            CollisionGroups = packet.ReadUInt32();
            PreviousPosition = packet.ReadFarPosition();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(CollisionGroups);
            hasher.Put(PreviousPosition);
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
            return base.ToString() + ", CollisionGroups=" + CollisionGroups + ", PreviousPosition=" + PreviousPosition;
        }

        #endregion
    }
}
