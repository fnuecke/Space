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
        #region Properties

        /// <summary>
        /// This components collision group. Components from the same group
        /// are <em>not</em> tested against each other.
        /// </summary>
        public int CollisionGroup { get; set; }

        /// <summary>
        /// The bounds of this collidable.
        /// </summary>
        public abstract Rectangle Bounds { get; }

        #endregion

        #region Fields

        /// <summary>
        /// Previous position of the underlying physics component (for sweep tests).
        /// </summary>
        protected Vector2 _previousPosition;

        #endregion

        #region Intersection

        /// <summary>
        /// Test if this collidable collides with the specified one.
        /// </summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <returns>Whether the two collide or not.</returns>
        public abstract bool Intersects(AbstractCollidable collidable);

        internal abstract bool Intersects(Vector2 extents, Vector2 previousPosition, Vector2 position);

        internal abstract bool Intersects(float radius, Vector2 previousPosition, Vector2 position);

        #endregion

        #region Logic
        
        /// <summary>
        /// Checks for collisions between this component and others given in the parameterization.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
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
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(CollisionParameterization);
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(CollisionGroup)
                .Write(_previousPosition);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            CollisionGroup = packet.ReadInt32();
            _previousPosition = packet.ReadVector2();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(CollisionGroup));
            hasher.Put(BitConverter.GetBytes(_previousPosition.X));
            hasher.Put(BitConverter.GetBytes(_previousPosition.Y));
        }

        #endregion
    }
}
