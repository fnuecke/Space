using System;
using Engine.Math;
using Engine.Physics.Intersection;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Implements a box, which has a size which is used to
    /// determine collisions.
    /// 
    /// <para>
    /// Requires: <c>StaticPhysics</c>.
    /// </para>
    /// </summary>
    public class CollidableBox : AbstractCollidable
    {
        #region Properties
        
        /// <summary>
        /// The bounds of this box.
        /// </summary>
        public FPoint Size { get; set; }

        #endregion

        #region Intersection

        public override bool Intersects(FPoint extents, FPoint previousPosition, FPoint position)
        {
            return AABBSweep.Test(
                this.Size, this.previousPosition, this.Entity.GetComponent<StaticPhysics>().Position,
                extents, previousPosition, position);
        }

        public override bool Intersects(Fixed radius, FPoint previousPosition, FPoint position)
        {
            return SphereAABBSweep.Test(
                radius, previousPosition, position,
                this.Size, this.previousPosition, this.Entity.GetComponent<StaticPhysics>().Position);
        }

        #endregion

        #region Serialization / Hashing

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

        #endregion
    }
}
