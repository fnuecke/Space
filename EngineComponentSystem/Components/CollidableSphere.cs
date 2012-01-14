using System;
using Engine.Physics.Intersection;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

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
        #region Fields
        
        /// <summary>
        /// The radius of this sphere.
        /// </summary>
        public float Radius;

        #endregion

        #region Constructor

        public CollidableSphere(float radius, uint groups)
            : base(groups)
        {
            this.Radius = radius;
        }

        public CollidableSphere(float radius)
            : this(radius, 0)
        {
        }

        public CollidableSphere(uint groups)
            : this(0, groups)
        {
        }

        public CollidableSphere()
            : this(0, 0)
        {
        }

        #endregion

        #region Intersection

        /// <summary>
        /// Test if this collidable collides with the specified one.
        /// </summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <returns>Whether the two collide or not.</returns>
        public override bool Intersects(AbstractCollidable collidable)
        {
            if (_previousPosition == Vector2.Zero)
            {
                _previousPosition = this.Entity.GetComponent<Transform>().Translation;
            }
            return collidable.Intersects(Radius, ref _previousPosition, ref this.Entity.GetComponent<Transform>().Translation);
        }

        internal override bool Intersects(ref Vector2 extents, ref Vector2 previousPosition, ref Vector2 position)
        {
            if (_previousPosition == Vector2.Zero)
            {
                _previousPosition = this.Entity.GetComponent<Transform>().Translation;
            }
            return SphereAABBSweep.Test(
                this.Radius, ref this._previousPosition, ref this.Entity.GetComponent<Transform>().Translation,
                ref extents, ref previousPosition, ref position);
        }

        internal override bool Intersects(float radius, ref Vector2 previousPosition, ref Vector2 position)
        {
            if (_previousPosition == Vector2.Zero)
            {
                _previousPosition = this.Entity.GetComponent<Transform>().Translation;
            }
            return SphereSweep.Test(
                this.Radius, ref this._previousPosition, ref this.Entity.GetComponent<Transform>().Translation,
                radius, ref previousPosition, ref position);
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

            Radius = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Radius));
        }

        #endregion

        #region Copying

        protected override bool ValidateType(AbstractComponent instance)
        {
            return instance is CollidableSphere;
        }

        protected override void CopyFields(AbstractComponent into, bool isShallowCopy)
        {
            base.CopyFields(into, isShallowCopy);

            if (!isShallowCopy)
            {
                var copy = (CollidableSphere)into;

                copy.Radius = Radius;
            }
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
