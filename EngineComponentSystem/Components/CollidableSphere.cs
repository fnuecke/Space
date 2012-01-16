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

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Radius);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Radius = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Radius));
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (CollidableSphere)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Radius = Radius;
            }

            return copy;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Radius = " + Radius.ToString();
        }

        #endregion
    }
}
