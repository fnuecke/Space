using System.Globalization;
using Engine.ComponentSystem.Components;
using Engine.Physics.Math;
using Engine.Serialization;
using Engine.XnaExtensions;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.Physics.Components
{
    /// <summary>
    /// A circular fixture with a set radius and offset to the body's local origin.
    /// </summary>
    public sealed class CircleFixture : Fixture
    {
        #region Fields

        /// <summary>
        /// The position relative to the local origin of the body this circle is attached to.
        /// </summary>
        internal LocalPoint Center;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="CircleFixture"/> class.
        /// </summary>
        public CircleFixture() : base(FixtureType.Circle)
        {
        }

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherCircle = (CircleFixture)other;
            Center = otherCircle.Center;

            return this;
        }

        /// <summary>
        /// Initializes the component with the specified offset relative to the body's
        /// local origin and the specified radius.
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

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Center = LocalPoint.Zero;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Test the specified point for containment in this fixture.
        /// </summary>
        /// <param name="localPoint">The point in local coordiantes.</param>
        /// <returns>Whether the point is contained in this fixture or not.</returns>
        public override bool TestPoint(Vector2 localPoint)
        {
            var d = Center - localPoint;
            return Vector2.Dot(d, d) <= Radius * Radius;
        }

        /// <summary>
        /// Get the mass data for this fixture. The mass data is based on the density and
        /// the shape. The rotational inertia is about the shape's origin. This operation
        /// may be expensive.
        /// </summary>
        /// <param name="mass">The mass of this fixture.</param>
        /// <param name="center">The center of mass relative to the local origin.</param>
        /// <param name="inertia">The inertia about the local origin.</param>
        internal override void GetMassData(out float mass, out LocalPoint center, out float inertia)
        {
            mass = Density * MathHelper.Pi * Radius * Radius;
            center = Center;

            // Inertia about the local origin.
            inertia = mass * (0.5f * Radius * Radius + Vector2.Dot(Center, Center));
        }

        /// <summary>
        /// Computes the global bounds of this fixture given the specified body transform.
        /// </summary>
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
                .Write(Center);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Center = packet.ReadVector2();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Center);
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
            return base.ToString() + ", Center=" + Center.X.ToString(CultureInfo.InvariantCulture) + ":" + Center.Y.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
