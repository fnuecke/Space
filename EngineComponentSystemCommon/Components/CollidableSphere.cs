using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.FarMath;
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
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public new static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

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
        /// <param name="sweep">Whether the collidable should perform sweeping
        /// intersection tests (for small, fast moving objects, like bullets).</param>
        public CollidableSphere Initialize(float radius, uint groups, bool sweep = false)
        {
            Initialize(groups, sweep);

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
        public override FarRectangle ComputeBounds()
        {
            return new FarRectangle(0, 0, Radius * 2, Radius * 2);
        }

        /// <summary>
        /// Test if this collidable collides with the specified one.
        /// </summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <param name="normal">The normal pointing to the other collidable at the
        /// time of collision (may differ from current direction due to sweep tests).</param>
        /// <returns>
        /// Whether the two collide or not.
        /// </returns>
        internal override bool Intersects(Collidable collidable, out Vector2 normal)
        {
            var currentPosition = ((Transform)Manager.GetComponent(Entity, Transform.TypeId)).Translation;
            if (ShouldSweep || collidable.ShouldSweep)
            {
                // Use sweep collision tests.
                return collidable.Intersects(Radius, ref PreviousPosition, ref currentPosition, out normal);
            }
            else
            {
                // Use simple collision tests.
                return collidable.Intersects(Radius, ref currentPosition, out normal);
            }
        }

        internal override bool Intersects(ref Vector2 extents, ref FarPosition position, out Vector2 normal)
        {
            var currentPosition = ((Transform)Manager.GetComponent(Entity, Transform.TypeId)).Translation;
            if (Intersection.Test(ref extents, ref position, Radius, ref currentPosition))
            {
                normal = (Vector2)(position - currentPosition);
                normal.Normalize();
                return true;
            }
            else
            {
                normal = Vector2.Zero;
                return false;
            }
        }

        internal override bool Intersects(float radius, ref FarPosition position, out Vector2 normal)
        {
            var currentPosition = ((Transform)Manager.GetComponent(Entity, Transform.TypeId)).Translation;
            if (Intersection.Test(Radius, ref currentPosition, radius, ref position))
            {
                normal = (Vector2)(position - currentPosition);
                normal.Normalize();
                return true;
            }
            else
            {
                normal = Vector2.Zero;
                return false;
            }
        }

        internal override bool Intersects(ref Vector2 extents, ref FarPosition previousPosition, ref FarPosition position, out Vector2 normal)
        {
            var currentPosition = ((Transform)Manager.GetComponent(Entity, Transform.TypeId)).Translation;
            float t;
            if (Intersection.Test(
                Radius, ref PreviousPosition, ref currentPosition,
                ref extents, ref previousPosition, ref position, out t))
            {
                var v0 = FarPosition.Lerp(previousPosition, position, t);
                var v1 = FarPosition.Lerp(PreviousPosition, currentPosition, t);
                normal = (Vector2)(v1 - v0);
                normal.Normalize();
                return true;
            }
            else
            {
                normal = Vector2.Zero;
                return false;
            }
        }

        internal override bool Intersects(float radius, ref FarPosition previousPosition, ref FarPosition position, out Vector2 normal)
        {
            var currentPosition = ((Transform)Manager.GetComponent(Entity, Transform.TypeId)).Translation;
            float t;
            if (Intersection.Test(
                Radius, ref PreviousPosition, ref currentPosition,
                radius, ref previousPosition, ref position, out t))
            {
                var v0 = FarPosition.Lerp(previousPosition, position, t);
                var v1 = FarPosition.Lerp(PreviousPosition, currentPosition, t);
                normal = (Vector2)(v1 - v0);
                normal.Normalize();
                return true;
            }
            else
            {
                normal = Vector2.Zero;
                return false;
            }
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
