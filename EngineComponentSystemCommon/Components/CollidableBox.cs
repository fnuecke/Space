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
    public class CollidableBox : Collidable
    {
        #region Fields
        
        /// <summary>
        /// The bounds of this box.
        /// </summary>
        public Vector2 Size;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override void Initialize(Component other)
        {
            base.Initialize(other);

            Size = ((CollidableBox)other).Size;
        }

        /// <summary>
        /// Initialize the component with the specified size and collision groups.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="groups">The collision groups.</param>
        public void Initialize(Vector2 size, uint groups)
        {
            base.Initialize(groups);

            Size = size;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Size = Vector2.Zero;
        }

        #endregion

        #region Intersection

        /// <summary>
        /// Test if this collidable collides with the specified one.
        /// </summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <returns>Whether the two collide or not.</returns>
        public override bool Intersects(Collidable collidable)
        {
            if (PreviousPosition == Vector2.Zero)
            {
                PreviousPosition = Manager.GetComponent<Transform>(Entity).Translation;
            }
            var currentPosition = Manager.GetComponent<Transform>(Entity).Translation;
            return collidable.Intersects(ref Size, ref PreviousPosition, ref currentPosition);
        }

        internal override bool Intersects(ref Vector2 extents, ref Vector2 previousPosition, ref Vector2 position)
        {
            if (PreviousPosition == Vector2.Zero)
            {
                PreviousPosition = Manager.GetComponent<Transform>(Entity).Translation;
            }
            var currentPosition = Manager.GetComponent<Transform>(Entity).Translation;
            return AABBSweep.Test(
                ref this.Size, ref this.PreviousPosition, ref currentPosition,
                ref extents, ref previousPosition, ref position);
        }

        internal override bool Intersects(float radius, ref Vector2 previousPosition, ref Vector2 position)
        {
            if (PreviousPosition == Vector2.Zero)
            {
                PreviousPosition = Manager.GetComponent<Transform>(Entity).Translation;
            }
            var currentPosition = Manager.GetComponent<Transform>(Entity).Translation;
            return SphereAABBSweep.Test(
                radius, ref previousPosition, ref position,
                ref this.Size, ref this.PreviousPosition, ref currentPosition);
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
                .Write(Size);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Size = packet.ReadVector2();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(Size.X));
            hasher.Put(BitConverter.GetBytes(Size.Y));
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
            return base.ToString() + ", Size = " + Size.ToString();
        }

        #endregion
    }
}
