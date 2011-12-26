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

        /// <summary>
        /// Test if this collidable collides with the specified one.
        /// </summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <returns>Whether the two collide or not.</returns>
        public override bool Intersects(AbstractCollidable collidable)
        {
            return collidable.Intersects(Radius, _previousPosition, this.Entity.GetComponent<Transform>().Translation);
        }

        internal override bool Intersects(FPoint extents, FPoint previousPosition, FPoint position)
        {
            return SphereAABBSweep.Test(
                this.Radius, this._previousPosition, this.Entity.GetComponent<Transform>().Translation,
                extents, previousPosition, position);
        }

        internal override bool Intersects(Fixed radius, FPoint previousPosition, FPoint position)
        {
            return SphereSweep.Test(
                this.Radius, this._previousPosition, this.Entity.GetComponent<Transform>().Translation,
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

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + Radius.ToString();
        }

        #endregion
    }
}
