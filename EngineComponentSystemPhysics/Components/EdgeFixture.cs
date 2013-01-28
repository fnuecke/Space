using Engine.ComponentSystem.Physics.Math;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.ComponentSystem.Physics.Components
{
    /// <summary>An edge fixture which defines a line with two end points, relative to the body's local origin.</summary>
    public sealed class EdgeFixture : Fixture
    {
        #region Fields

        /// <summary>The end points of the edge, relative to the body's local origin, as well as the ghost vertices.</summary>
        internal LocalPoint Vertex0, Vertex1, Vertex2, Vertex3;

        /// <summary>Whether this edge has ghost vertices.</summary>
        internal bool HasVertex0, HasVertex3;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="EdgeFixture"/> class.
        /// </summary>
        public EdgeFixture() : base(FixtureType.Edge)
        {
            Radius = Settings.PolygonRadius;
        }

        /// <summary>Initializes the edge with specified end vertices. This will not use ghost vertices.</summary>
        /// <param name="v1">The first vertex.</param>
        /// <param name="v2">The the second vertex.</param>
        /// <returns></returns>
        public EdgeFixture InitializeShape(LocalPoint v1, LocalPoint v2)
        {
            Vertex0 = Vector2.Zero;
            Vertex1 = v1;
            Vertex2 = v2;
            Vertex3 = Vector2.Zero;
            HasVertex0 = false;
            HasVertex3 = false;

            Synchronize();

            return this;
        }

        /// <summary>
        ///     Initialize the component with the specified values. Note that this does not automatically change the mass of the
        ///     body. You have to call
        ///     <see cref="Body.ResetMassData"/> to update it.
        /// </summary>
        /// <param name="density">The density.</param>
        /// <param name="friction">The friction.</param>
        /// <param name="restitution">The restitution.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> this fixture is marked as a sensor.
        /// </param>
        /// <param name="collisionCategory">The collision groups for this fixture.</param>
        /// <param name="collisionMask">The collision groups we collide with.</param>
        /// <returns></returns>
        public override Fixture Initialize(
            float density = 0,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionCategory = 1,
            uint collisionMask = 0xFFFFFFFF)
        {
            // Edges can't have density because they have no area.
// ReSharper disable CompareOfFloatsByEqualityOperator Intentional
            System.Diagnostics.Debug.Assert(density == 0f);
// ReSharper restore CompareOfFloatsByEqualityOperator
            base.Initialize(0, friction, restitution, isSensor, collisionCategory, collisionMask);

            return this;
        }

        /// <summary>Initializes the specified vertices. This uses ghost vertices.</summary>
        /// <param name="v0">The first ghost vertex.</param>
        /// <param name="v1">The first vertex.</param>
        /// <param name="v2">The second vertex.</param>
        /// <param name="v3">The second ghost vertex.</param>
        /// <returns></returns>
        public EdgeFixture InitializeShape(LocalPoint v0, LocalPoint v1, LocalPoint v2, LocalPoint v3)
        {
            Vertex0 = v0;
            Vertex1 = v1;
            Vertex2 = v2;
            Vertex3 = v3;
            HasVertex0 = true;
            HasVertex3 = true;

            Synchronize();

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Vertex0 = Vertex1 = Vertex2 = Vertex3 = Vector2.Zero;
            HasVertex0 = HasVertex3 = false;
        }

        #endregion

        #region Methods

        /// <summary>Test the specified point for containment in this fixture.</summary>
        /// <param name="localPoint">The point in local coordinates.</param>
        /// <returns>Whether the point is contained in this fixture or not.</returns>
        public override bool TestPoint(Vector2 localPoint)
        {
            return false;
        }

        /// <summary>
        ///     Get the mass data for this fixture. The mass data is based on the density and the shape. The rotational
        ///     inertia is about the shape's origin. This operation may be expensive.
        /// </summary>
        /// <param name="mass">The mass of this fixture.</param>
        /// <param name="center">The center of mass relative to the local origin.</param>
        /// <param name="inertia">The inertia about the local origin.</param>
        internal override void GetMassData(out float mass, out LocalPoint center, out float inertia)
        {
            mass = 0;
            center = 0.5f * (Vertex1 + Vertex2);
            inertia = 0;
        }

        /// <summary>Computes the global bounds of this fixture given the specified body transform.</summary>
        /// <param name="transform">The world transform of the body.</param>
        /// <returns>The global bounds of this fixture.</returns>
        internal override WorldBounds ComputeBounds(WorldTransform transform)
        {
            var v1 = transform.ToGlobal(Vertex1);
            var v2 = transform.ToGlobal(Vertex2);

            var lowerX = (v1.X < v2.X) ? v1.X : v2.X;
            var lowerY = (v1.Y < v2.Y) ? v1.Y : v2.Y;
            var upperX = (v1.X > v2.X) ? v1.X : v2.X;
            var upperY = (v1.Y > v2.Y) ? v1.Y : v2.Y;

// ReSharper disable RedundantCast Necessary for FarPhysics.
            var sizeX = (float) (upperX - lowerX) + 2 * Radius;
            var sizeY = (float) (upperY - lowerY) + 2 * Radius;
// ReSharper restore RedundantCast

            WorldBounds bounds;
            bounds.X = lowerX - Radius;
            bounds.Y = lowerY - Radius;
            bounds.Width = sizeX;
            bounds.Height = sizeY;
            return bounds;
        }

        #endregion
    }
}