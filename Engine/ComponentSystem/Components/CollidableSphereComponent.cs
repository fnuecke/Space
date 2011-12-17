using System;
using Engine.Math;
using Engine.Physics.Intersection;

namespace Engine.ComponentSystem.Components
{
    public class CollidableSphereComponent : AbstractCollidableComponent
    {
        #region Properties
        
        /// <summary>
        /// The radius of this sphere.
        /// </summary>
        public Fixed Radius { get; set; }

        #endregion

        public CollidableSphereComponent(PhysicsComponent physicsComponent)
            : base(physicsComponent)
        {
        }

        public CollidableSphereComponent()
        {
        }

        #region Intersection

        public override bool Intersects(FPoint extents, FPoint previousPosition, FPoint position)
        {
            return SphereAABBSweep.Test(this.Radius, this.PreviousPosition, this.PhysicsComponent.Position,
                extents, previousPosition, position);
        }

        public override bool Intersects(Fixed radius, FPoint previousPosition, FPoint position)
        {
            return SphereSweep.Test(this.Radius, this.PreviousPosition, this.PhysicsComponent.Position,
                radius, previousPosition, position);
        }

        #endregion

        public override void Packetize(Serialization.Packet packet)
        {
            base.Packetize(packet);
            packet.Write(Radius);
        }

        public override void Depacketize(Serialization.Packet packet, Serialization.IPacketizerContext context)
        {
            base.Depacketize(packet, context);
            Radius = packet.ReadFixed();
        }

        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);
            hasher.Put(BitConverter.GetBytes(Radius.RawValue));
        }
    }
}
