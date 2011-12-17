using System;
using Engine.Math;
using Engine.Physics.Intersection;

namespace Engine.ComponentSystem.Components
{
    public class CollidableBoxComponent : AbstractCollidableComponent
    {
        #region Properties
        
        /// <summary>
        /// The bounds of this box.
        /// </summary>
        public FPoint Size { get; set; }

        #endregion

        public CollidableBoxComponent(PhysicsComponent physicsComponent)
            : base(physicsComponent)
        {
        }

        public CollidableBoxComponent()
        {
        }

        #region Intersection

        public override bool Intersects(FPoint extents, FPoint previousPosition, FPoint position)
        {
            return AABBSweep.Test(this.Size, this.PreviousPosition, this.PhysicsComponent.Position,
                extents, previousPosition, position);
        }

        public override bool Intersects(Fixed radius, FPoint previousPosition, FPoint position)
        {
            return SphereAABBSweep.Test(radius, previousPosition, position,
                this.Size, this.PreviousPosition, this.PhysicsComponent.Position);
        }

        #endregion

        public override void Packetize(Serialization.Packet packet)
        {
            base.Packetize(packet);
            packet.Write(Size);
        }

        public override void Depacketize(Serialization.Packet packet, Serialization.IPacketizerContext context)
        {
            base.Depacketize(packet, context);
            Size = packet.ReadFPoint();
        }

        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);
            hasher.Put(BitConverter.GetBytes(Size.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Size.Y.RawValue));
        }
    }
}
