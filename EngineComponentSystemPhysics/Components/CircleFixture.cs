﻿using Engine.ComponentSystem.Physics.Math;
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
    /// <summary>A circular fixture with a set radius and offset to the body's local origin.</summary>
    public sealed class CircleFixture : Fixture
    {
        #region Fields

        /// <summary>The position relative to the local origin of the body this circle is attached to.</summary>
        internal LocalPoint Center;

        #endregion

        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="CircleFixture"/> class.
        /// </summary>
        public CircleFixture() : base(FixtureType.Circle) {}

        /// <summary>
        ///     Initializes the component with the specified offset relative to the body's local origin and the specified
        ///     radius.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public CircleFixture InitializeShape(float radius, LocalPoint position)
        {
            Radius = radius;
            Center = position;

            Synchronize();

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Center = LocalPoint.Zero;
        }

        #endregion

        #region Methods

        /// <summary>Test the specified point for containment in this fixture.</summary>
        /// <param name="localPoint">The point in local coordinates.</param>
        /// <returns>Whether the point is contained in this fixture or not.</returns>
        public override bool TestPoint(Vector2 localPoint)
        {
            var d = Center - localPoint;
            return Vector2Util.Dot(ref d, ref d) <= Radius * Radius;
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
            mass = Density * MathHelper.Pi * Radius * Radius;
            center = Center;

            // Inertia about the local origin.
            inertia = mass * (0.5f * Radius * Radius + Vector2Util.Dot(ref Center, ref Center));
        }

        /// <summary>Computes the global bounds of this fixture given the specified body transform.</summary>
        /// <param name="transform">The world transform of the body.</param>
        /// <returns>The global bounds of this fixture.</returns>
        internal override WorldBounds ComputeBounds(WorldTransform transform)
        {
            var p = transform.Translation + transform.Rotation * Center;

            WorldBounds bounds;
            bounds.X = p.X - Radius;
            bounds.Y = p.Y - Radius;
            bounds.Width = Radius + Radius;
            bounds.Height = Radius + Radius;
            return bounds;
        }

        #endregion
    }
}