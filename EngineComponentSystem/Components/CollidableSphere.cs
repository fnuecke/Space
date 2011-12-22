using System;
using Engine.Math;
using Engine.Physics.Intersection;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Implements a sphere, which has a radius which is used to
    /// determine collisions.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public class CollidableSphere : AbstractCollidable
    {
        #region Properties
        
        /// <summary>
        /// The radius of this sphere.
        /// </summary>
        public Fixed Radius { get; set; }

        #endregion

        #region Intersection

        public override bool Intersects(FPoint extents, FPoint previousPosition, FPoint position)
        {
            return SphereAABBSweep.Test(
                this.Radius, this.previousPosition, this.Entity.GetComponent<Transform>().Translation,
                extents, previousPosition, position);
        }

        public override bool Intersects(Fixed radius, FPoint previousPosition, FPoint position)
        {
            return SphereSweep.Test(
                this.Radius, this.previousPosition, this.Entity.GetComponent<Transform>().Translation,
                radius, previousPosition, position);
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Radius);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Radius = packet.ReadFixed();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Radius.RawValue));
        }

        #endregion
    }
}
