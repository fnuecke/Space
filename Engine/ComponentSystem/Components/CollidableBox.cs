using System;
using Engine.Math;
using Engine.Physics.Intersection;

namespace Engine.ComponentSystem.Components
{
    public class CollidableBox : AbstractCollidable
    {
        #region Properties
        
        /// <summary>
        /// The bounds of this box.
        /// </summary>
        public FPoint Size { get; set; }

        #endregion

        public CollidableBox(StaticPhysics staticPhysicsComponent)
            : base(staticPhysicsComponent)
        {
        }

        #region Intersection

        public override bool Intersects(FPoint extents, FPoint previousPosition, FPoint position)
        {
            return AABBSweep.Test(
                this.Size, this.previousPosition, this.StaticPhysicsComponent.Position,
                extents, previousPosition, position);
        }

        public override bool Intersects(Fixed radius, FPoint previousPosition, FPoint position)
        {
            return SphereAABBSweep.Test(
                radius, previousPosition, position,
                this.Size, this.previousPosition, this.StaticPhysicsComponent.Position);
        }

        #endregion

        public override void Packetize(Serialization.Packet packet)
        {
            base.Packetize(packet);
            packet.Write(Size);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);
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
