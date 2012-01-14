using System;
using Engine.Physics.Intersection;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

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
        #region Fields
        
        /// <summary>
        /// The bounds of this box.
        /// </summary>
        public Vector2 Size;

        #endregion
        
        #region Constructor

        public CollidableBox(Vector2 size, uint groups)
            : base(groups)
        {
            this.Size = size;
        }

        public CollidableBox(Vector2 size)
            : this(size, 0)
        {
        }

        public CollidableBox(uint groups)
            : this(Vector2.Zero, groups)
        {
        }

        public CollidableBox()
            : this(Vector2.Zero, 0)
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
            return collidable.Intersects(ref Size, ref _previousPosition, ref this.Entity.GetComponent<Transform>().Translation);
        }

        internal override bool Intersects(ref Vector2 extents, ref Vector2 previousPosition, ref Vector2 position)
        {
            if (_previousPosition == Vector2.Zero)
            {
                _previousPosition = this.Entity.GetComponent<Transform>().Translation;
            }
            return AABBSweep.Test(
                ref this.Size, ref this._previousPosition, ref this.Entity.GetComponent<Transform>().Translation,
                ref extents, ref previousPosition, ref position);
        }

        internal override bool Intersects(float radius, ref Vector2 previousPosition, ref Vector2 position)
        {
            if (_previousPosition == Vector2.Zero)
            {
                _previousPosition = this.Entity.GetComponent<Transform>().Translation;
            }
            return SphereAABBSweep.Test(
                radius, ref previousPosition, ref position,
                ref this.Size, ref this._previousPosition, ref this.Entity.GetComponent<Transform>().Translation);
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

            Size = packet.ReadVector2();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Size.X));
            hasher.Put(BitConverter.GetBytes(Size.Y));
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (CollidableBox)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Size = Size;
            }

            return copy;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + Size.ToString();
        }

        #endregion
    }
}
