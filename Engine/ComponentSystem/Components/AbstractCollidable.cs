using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;
using Engine.Physics;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Base class for components that implement collision logic.
    /// 
    /// <para>
    /// Requires: <c>StaticPhysics</c>.
    /// </para>
    /// </summary>
    public abstract class AbstractCollidable : AbstractComponent, ICollideable
    {
        #region Fields

        /// <summary>
        /// Previous position of the underlying physics component (for sweep tests).
        /// </summary>
        protected FPoint previousPosition;

        #endregion

        #region Intersection

        public abstract bool Intersects(FPoint extents, FPoint previousPosition, FPoint position);

        public abstract bool Intersects(Fixed radius, FPoint previousPosition, FPoint position);

        #endregion

        #region Logic
        
        /// <summary>
        /// Checks for collisions between this component and others given in the parameterization.
        /// </summary>
        /// <param name="parameterization">the parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            // Only do this expensive check (see implementation) in debug mode,
            // as it should not happen that this is of an invalid type anyway.
            base.Update(parameterization);
#endif
            var p = (CollisionParameterization)parameterization;
            // TODO parameterization must contain list of objects to test collision with and possibility to return collision results

            previousPosition = Entity.GetComponent<StaticPhysics>().Position;
        }

        /// <summary>
        /// Accepts <c>CollisionParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType.Equals(typeof(CollisionParameterization));
        }

        #endregion

        #region Serialization / Hashing

        public override void Packetize(Serialization.Packet packet)
        {
            packet.Write(previousPosition);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            previousPosition = packet.ReadFPoint();
        }

        public override void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(previousPosition.X.RawValue));
            hasher.Put(BitConverter.GetBytes(previousPosition.Y.RawValue));
        }

        #endregion
    }
}
