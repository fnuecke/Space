using System;
using Engine.Math;
using Engine.Physics.Intersection;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Implements a sphere, which has a radius which is used to
    /// determine collisions.
    /// 
    /// <para>
    /// Requires: <c>StaticPhysics</c>.
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
                this.Radius, this.previousPosition, this.Entity.GetComponent<StaticPhysics>().Position,
                extents, previousPosition, position);
        }

        public override bool Intersects(Fixed radius, FPoint previousPosition, FPoint position)
        {
            return SphereSweep.Test(
                this.Radius, this.previousPosition, this.Entity.GetComponent<StaticPhysics>().Position,
                radius, previousPosition, position);
        }

        #endregion

        #region Serialization / Hashing

        public override void Packetize(Serialization.Packet packet)
        {
            base.Packetize(packet);
            packet.Write(Radius);
        }

        public override void Depacketize(Serialization.Packet packet)
        {
            base.Depacketize(packet);
            Radius = packet.ReadFixed();
        }

        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);
            hasher.Put(BitConverter.GetBytes(Radius.RawValue));
        }

        #endregion
    }
}
