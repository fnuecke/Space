using System;
using Engine.Math;
using Engine.Physics.Intersection;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Implements a box, which has a size which is used to
    /// determine collisions.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
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

        /// <summary>
        /// Test if this collidable collides with the specified one.
        /// </summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <returns>Whether the two collide or not.</returns>
        public override bool Intersects(AbstractCollidable collidable)
        {
            return collidable.Intersects(Size, _previousPosition, this.Entity.GetComponent<Transform>().Translation);
        }

        internal override bool Intersects(FPoint extents, FPoint previousPosition, FPoint position)
        {
            return AABBSweep.Test(
                this.Size, this._previousPosition, this.Entity.GetComponent<Transform>().Translation,
                extents, previousPosition, position);
        }

        internal override bool Intersects(Fixed radius, FPoint previousPosition, FPoint position)
        {
            return SphereAABBSweep.Test(
                radius, previousPosition, position,
                this.Size, this._previousPosition, this.Entity.GetComponent<Transform>().Translation);
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Size);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Size = packet.ReadFPoint();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Size.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Size.Y.RawValue));
        }

        #endregion
    }
}
