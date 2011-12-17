using System;
using Engine.Math;
using Engine.Physics;

namespace Engine.ComponentSystem.Components
{
    public abstract class AbstractCollidableComponent : IComponent, ICollideable
    {
        #region Properties

        /// <summary>
        /// Previous position of the underlying physics component (for sweep tests).
        /// </summary>
        public FPoint PreviousPosition { get; set; }

        /// <summary>
        /// The underlying physics component we do collision checks for.
        /// </summary>
        public PhysicsComponent PhysicsComponent { get; private set; }

        #endregion

        public AbstractCollidableComponent(PhysicsComponent physicsComponent)
        {
            this.PreviousPosition = physicsComponent.Position;
            this.PhysicsComponent = physicsComponent;
        }

        public AbstractCollidableComponent()
        {
        }

        #region Intersection

        public abstract bool Intersects(FPoint extents, FPoint previousPosition, FPoint position);

        public abstract bool Intersects(Fixed radius, FPoint previousPosition, FPoint position);

        #endregion

        public virtual void Update(object parameterization)
        {
            // TODO parameterization must contain list of objects to test collision with and possibility to return collision results

            PreviousPosition = PhysicsComponent.Position;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public virtual void Packetize(Serialization.Packet packet)
        {
            packet.Write(PreviousPosition);
        }

        public virtual void Depacketize(Serialization.Packet packet, Serialization.IPacketizerContext context)
        {
            PreviousPosition = packet.ReadFPoint();
        }

        public virtual void Hash(Util.Hasher hasher)
        {
            hasher.Put(BitConverter.GetBytes(PreviousPosition.X.RawValue));
            hasher.Put(BitConverter.GetBytes(PreviousPosition.Y.RawValue));
        }
    }
}
