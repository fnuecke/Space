using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;
using Engine.Physics;

namespace Engine.ComponentSystem.Components
{
    public abstract class AbstractCollidable : AbstractComponent, ICollideable
    {
        #region Properties

        /// <summary>
        /// The underlying physics component we do collision checks for.
        /// </summary>
        public StaticPhysics StaticPhysicsComponent { get; private set; }

        #endregion

        /// <summary>
        /// Previous position of the underlying physics component (for sweep tests).
        /// </summary>
        protected FPoint previousPosition;

        public AbstractCollidable(StaticPhysics staticPhysicsComponent)
        {
            this.previousPosition = staticPhysicsComponent.Position;
            this.StaticPhysicsComponent = staticPhysicsComponent;
        }

        #region Intersection

        public abstract bool Intersects(FPoint extents, FPoint previousPosition, FPoint position);

        public abstract bool Intersects(Fixed radius, FPoint previousPosition, FPoint position);

        #endregion

        public override void Update(object parameterization)
        {
#if DEBUG
            // Only do this expensive check in debug mode, as this should not happen anyway.
            if (!SupportsParameterization(parameterization))
            {
                throw new System.ArgumentException("parameterization");
            }
#endif
            var p = (CollisionParameterization)parameterization;
            // TODO parameterization must contain list of objects to test collision with and possibility to return collision results

            previousPosition = StaticPhysicsComponent.Position;
        }

        public override bool SupportsParameterization(object parameterization)
        {
            return parameterization is CollisionParameterization;
        }

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
    }
}
