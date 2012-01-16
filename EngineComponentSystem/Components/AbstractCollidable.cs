using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Base class for components that implement collision logic.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public abstract class AbstractCollidable : AbstractComponent
    {
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
        protected Vector2 _previousPosition;

        #endregion

        #region Constructor

        protected AbstractCollidable(uint groups)
        {
            this.CollisionGroups = groups;
        }

        protected AbstractCollidable()
            : this(0)
        {
        }

        #endregion

        #region Intersection

        /// <summary>
        /// Test if this collidable collides with the specified one.
        /// </summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <returns>Whether the two collide or not.</returns>
        public abstract bool Intersects(AbstractCollidable collidable);

        internal abstract bool Intersects(ref Vector2 extents, ref Vector2 previousPosition, ref Vector2 position);

        internal abstract bool Intersects(float radius, ref Vector2 previousPosition, ref Vector2 position);

        #endregion

        #region Logic
        
        /// <summary>
        /// Checks for collisions between this component and others given in the parameterization.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Update(object parameterization)
        {
            // Update our previous position.
            var transform = Entity.GetComponent<Transform>();
            if (transform != null)
            {
                _previousPosition = transform.Translation;
            }
        }

        /// <summary>
        /// Accepts <c>CollisionParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsUpdateParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(CollisionParameterization);
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
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(CollisionGroups)
                .Write(_previousPosition);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            CollisionGroups = packet.ReadUInt32();
            _previousPosition = packet.ReadVector2();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(CollisionGroups));
            hasher.Put(BitConverter.GetBytes(_previousPosition.X));
            hasher.Put(BitConverter.GetBytes(_previousPosition.Y));
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (AbstractCollidable)base.DeepCopy(into);

            if (copy == into)
            {
                copy.CollisionGroups = CollisionGroups;
                copy._previousPosition = _previousPosition;
            }

            return copy;
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
            return base.ToString() + ", CollisionGroups = " + CollisionGroups.ToString() + ", PreviousPosition = " + _previousPosition.ToString();
        }

        #endregion
    }
}
