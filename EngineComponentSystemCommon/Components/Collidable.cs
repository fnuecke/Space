using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    ///     Base class for components that implement collision logic.
    ///     <para>
    ///         Requires: <c>Transform</c>.
    ///     </para>
    /// </summary>
    public abstract class Collidable : Component
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region States

        /// <summary>Possible states of the collidable.</summary>
        public enum CollisionState
        {
            /// <summary>No other collidables nearby.</summary>
            None,

            /// <summary>One or more collidables nearby, can collide (bounds overlap).</summary>
            Contact,

            /// <summary>Colliding with one or more other collidables.</summary>
            Collides
        }

        #endregion

        #region Fields

        /// <summary>
        ///     This bit mask representing the collision groups this component is part of. Components sharing at least one
        ///     group will not be tested against each other.
        /// </summary>
        public uint CollisionGroups;

        /// <summary>
        ///     Whether intersection tests involving this collidable should use a sweeping intersection test (relatively small
        ///     object moving at potentially high speeds), e.g. bullets.
        /// </summary>
        public bool ShouldSweep;

        /// <summary>Previous position of the underlying physics component (for sweep tests).</summary>
        public FarPosition PreviousPosition;

        /// <summary>
        ///     This field flags whether the component is currently (in this update cycle) colliding with another one. It is
        ///     set each update by the collision system. It is intended to be used for debug rendering.
        /// </summary>
        public CollisionState State;

        /// <summary>Start of the list of contacts this collidable is involved in.</summary>
        internal int ContactList = -1;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherCollidable = (Collidable) other;
            CollisionGroups = otherCollidable.CollisionGroups;
            ShouldSweep = otherCollidable.ShouldSweep;
            PreviousPosition = otherCollidable.PreviousPosition;

            return this;
        }

        /// <summary>Initialize the component with specified collision groups.</summary>
        /// <param name="groups">The groups.</param>
        /// <param name="sweep">
        ///     Whether the collidable should perform sweeping intersection tests (for small, fast moving objects,
        ///     like bullets).
        /// </param>
        /// <returns></returns>
        protected Collidable Initialize(uint groups, bool sweep = false)
        {
            CollisionGroups = groups;
            ShouldSweep = sweep;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            CollisionGroups = 0;
            ShouldSweep = false;
            PreviousPosition = FarPosition.Zero;
            ContactList = -1;
        }

        #endregion

        #region Intersection

        /// <summary>Computes the current minimal bounding box for this collidable.</summary>
        /// <returns>The minimal bounding box for this object.</returns>
        public abstract FarRectangle ComputeBounds();

        /// <summary>Test if this collidable collides with the specified one.</summary>
        /// <param name="collidable">The other collidable to test against.</param>
        /// <param name="normal">
        ///     The normal pointing to the other collidable at the time of collision (may differ from current
        ///     direction due to sweep tests).
        /// </param>
        /// <returns>Whether the two collide or not.</returns>
        internal abstract bool Intersects(Collidable collidable, out Vector2 normal);

        #region Pairwise matching for shape type collision, normal and sweep variants

        // Simple box intersection.
        internal abstract bool Intersects(ref Vector2 extents, ref FarPosition position, out Vector2 normal);

        // Simple sphere intersection.
        internal abstract bool Intersects(float radius, ref FarPosition position, out Vector2 normal);

        // Sweep box intersection.
        internal abstract bool Intersects(
            ref Vector2 extents, ref FarPosition previousPosition, ref FarPosition position, out Vector2 normal);

        // Sweep sphere intersection.
        internal abstract bool Intersects(
            float radius, ref FarPosition previousPosition, ref FarPosition position, out Vector2 normal);

        #endregion

        #endregion
    }
}