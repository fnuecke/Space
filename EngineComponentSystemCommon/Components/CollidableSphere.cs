using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.Physics.Intersection;
using Engine.Serialization;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// Implements a sphere, which has a radius which is used to
    /// determine collisions.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public sealed class CollidableSphere : Collidable
    {
        #region Fields
        
        /// <summary>
        /// The radius of this sphere. Must not be changed after initialization.
        /// </summary>
        public float Radius;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            Radius = ((CollidableSphere)other).Radius;

            return this;
        }

        /// <summary>
        /// Initialize the component with the specified radius and collision groups.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="groups">The collision groups.</param>
        public CollidableSphere Initialize(float radius, uint groups)
        {
            Initialize(groups);

            Radius = radius;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Radius = 0;
        }

        #endregion

        #region Intersection

        /// <summary>
        /// Computes the current minimal bounding box for this collidable.
        /// </summary>
        /// <returns>The minimal boundsing box for this object.</returns>
        public override Rectangle ComputeBounds()
        {
            return new Rectangle {Height = (int)(Radius * 2), Width = (int)(Radius * 2)};
        }

        /// <summary>
        /// Test if this collidable collides with the specified one.
        /// </summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <returns>Whether the two collide or not.</returns>
        public override bool Intersects(Collidable collidable)
        {
            var currentPosition = Manager.GetComponent<Transform>(Entity).Translation;
            return collidable.Intersects(Radius, ref PreviousPosition, ref currentPosition);
        }

        internal override bool Intersects(ref Vector2 extents, ref Vector2 previousPosition, ref Vector2 position)
        {
            var currentPosition = Manager.GetComponent<Transform>(Entity).Translation;
            return SphereAABBSweep.Test(
                Radius, ref PreviousPosition, ref currentPosition,
                ref extents, ref previousPosition, ref position);
        }

        internal override bool Intersects(float radius, ref Vector2 previousPosition, ref Vector2 position)
        {
            var currentPosition = Manager.GetComponent<Transform>(Entity).Translation;
            return SphereSweep.Test(
                Radius, ref PreviousPosition, ref currentPosition,
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

            hasher.Put(Radius);
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
            return base.ToString() + ", Radius=" + Radius.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
