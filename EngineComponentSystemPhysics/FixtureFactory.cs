using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Physics.Components;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;

#endif

namespace Engine.ComponentSystem.Physics
{
    public static class FixtureFactory
    {
        #region Circle

        /// <summary>Attaches a circle fixture to the specified entity.</summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="localPosition">The center of the circle relative to the body's local origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> the created fixture is marked as a sensor (i.e. it will fire collision events but the
        ///     collision will not be handled by the solver).
        /// </param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <returns>The created fixture.</returns>
        public static CircleFixture AttachCircle(
            this IManager manager,
            Body body,
            LocalPoint localPosition,
            float radius,
            float density = 0,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            var fixture = manager.AddComponent<CircleFixture>(body.Entity);
            fixture.Initialize(density, friction, restitution, isSensor, collisionGroups);
            fixture.InitializeShape(radius, localPosition);

            // Recompute mass data if necessary.
            if (density > 0)
            {
                body.ResetMassData();
            }

            return fixture;
        }

        /// <summary>Attaches a circle fixture to the specified entity.</summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> the created fixture is marked as a sensor (i.e. it will fire collision events but the
        ///     collision will not be handled by the solver).
        /// </param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <returns>The created fixture.</returns>
        public static CircleFixture AttachCircle(
            this IManager manager,
            Body body,
            float radius,
            float density = 0,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            return manager.AttachCircle(
                body,
                LocalPoint.Zero,
                radius,
                density,
                friction,
                restitution,
                isSensor,
                collisionGroups);
        }

        #endregion

        #region Edge

        /// <summary>Attaches an edge fixture to the specified entity.</summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> the created fixture is marked as a sensor (i.e. it will fire collision events but the
        ///     collision will not be handled by the solver).
        /// </param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <returns>The created fixture.</returns>
        public static EdgeFixture AttachEdge(
            this IManager manager,
            Body body,
            LocalPoint localStart,
            LocalPoint localEnd,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            var fixture = manager.AddComponent<EdgeFixture>(body.Entity);
            fixture.Initialize(
                friction: friction,
                restitution: restitution,
                isSensor: isSensor,
                collisionGroups: collisionGroups);
            fixture.InitializeShape(localStart, localEnd);

            return fixture;
        }

        /// <summary>Attaches an edge fixture to the specified entity.</summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="ghostStart">The ghost point for the start point, for chained edges.</param>
        /// <param name="localStart">The start point of the edge relative to the body's local origin.</param>
        /// <param name="localEnd">The end point of the shape relative to the body's local origin.</param>
        /// <param name="ghostEnd">The ghost point for end point, for chained edges.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> the created fixture is marked as a sensor (i.e. it will fire collision events but the
        ///     collision will not be handled by the solver).
        /// </param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <returns>The created fixture.</returns>
        public static EdgeFixture AttachEdge(
            this IManager manager,
            Body body,
            LocalPoint ghostStart,
            LocalPoint localStart,
            LocalPoint localEnd,
            LocalPoint ghostEnd,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            var fixture = manager.AddComponent<EdgeFixture>(body.Entity);
            fixture.Initialize(
                friction: friction,
                restitution: restitution,
                isSensor: isSensor,
                collisionGroups: collisionGroups);
            fixture.InitializeShape(ghostStart, localStart, localEnd, ghostEnd);

            return fixture;
        }

        #endregion

        #region Chain / Loop

        /// <summary>
        ///     Creates an edge loop with the specified parameters. A loop consists in fact of multiple edge fixtures with
        ///     neighborhood information, which avoid collisions with inner vertices. Note that is not actually Box2D's
        ///     implementation of a chain, but simply a factory for creating a single edge fixture per chain link.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="body">The body to attach the fixtures to.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> the created fixture is marked as a sensor (i.e. it will fire collision events but the
        ///     collision will not be handled by the solver).
        /// </param>
        /// <param name="collisionGroups">The collision groups.</param>
        public static void AttachLoop(
            this IManager manager,
            Body body,
            IList<LocalPoint> vertices,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            ValidateVertices(vertices, 3);

            manager.AttachLoopUnchecked(body, vertices, friction, restitution, isSensor, collisionGroups);
        }

        /// <summary>
        ///     Creates an edge chain with the specified parameters. A chain consists in fact of multiple edge fixtures with
        ///     neighborhood information, which avoid collisions with inner vertices. Note that is not actually Box2D's
        ///     implementation of a chain, but simply a factory for creating a single edge fixture per chain link.
        /// </summary>
        /// <param name="manager">The manager to create the object in.</param>
        /// <param name="body">The body to attach the fixtures to.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="friction">The friction of the fixture.</param>
        /// <param name="restitution">The restitution of the fixture.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> the created fixture is marked as a sensor (i.e. it will fire collision events but the
        ///     collision will not be handled by the solver).
        /// </param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <returns>A proxy object that may be used to interact with the created chain.</returns>
        public static Chain AttachChain(
            this IManager manager,
            Body body,
            IList<LocalPoint> vertices,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            ValidateVertices(vertices, 2);

            return manager.AttachChainUnchecked(body, vertices, friction, restitution, isSensor, collisionGroups);
        }

        /// <summary>Validates vertex input.</summary>
        internal static void ValidateVertices(IList<LocalPoint> vertices, int minCount)
        {
            if (vertices == null)
            {
                throw new ArgumentNullException("vertices");
            }
            if (vertices.Count < minCount)
            {
                throw new ArgumentException("Too few vertices to create a chain.", "vertices");
            }
            for (var i = 1; i < vertices.Count; ++i)
            {
                var v1 = vertices[i - 1];
                var v2 = vertices[i];
                if (LocalPoint.DistanceSquared(v1, v2) <= Settings.LinearSlop * Settings.LinearSlop)
                {
                    throw new ArgumentException("Some vertices are too close together.", "vertices");
                }
            }
        }

        /// <summary>Unchecked attach logic to avoid duplicate input validation.</summary>
        internal static void AttachLoopUnchecked(
            this IManager manager,
            Body body,
            IList<LocalPoint> vertices,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            for (var i = 0; i < vertices.Count; ++i)
            {
                var ghostStart = vertices[(i - 1 + vertices.Count) % vertices.Count];
                var start = vertices[i];
                var end = vertices[(i + 1) % vertices.Count];
                var ghostEnd = vertices[(i + 2) % vertices.Count];
                manager.AttachEdge(
                    body,
                    ghostStart,
                    start,
                    end,
                    ghostEnd,
                    friction,
                    restitution,
                    isSensor,
                    collisionGroups);
            }
        }

        /// <summary>Unchecked attach logic to avoid duplicate input validation.</summary>
        internal static Chain AttachChainUnchecked(
            this IManager manager,
            Body body,
            IList<LocalPoint> vertices,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            // See if it's just a single edge.
            if (vertices.Count == 2)
            {
                var edge = manager.AttachEdge(body, vertices[0], vertices[1], friction, restitution);
                return new Chain(body, edge, edge);
            }

            var head = manager.AttachEdge(
                body,
                vertices[0],
                vertices[1],
                friction,
                restitution,
                isSensor,
                collisionGroups);
            head.Vertex3 = vertices[2];
            head.HasVertex3 = true;

            for (var i = 1; i < vertices.Count - 2; ++i)
            {
                manager.AttachEdge(
                    body,
                    vertices[i - 1],
                    vertices[i],
                    vertices[i + 1],
                    vertices[i + 2],
                    friction,
                    restitution,
                    isSensor,
                    collisionGroups);
            }

            var tail = manager.AttachEdge(
                body,
                vertices[vertices.Count - 2],
                vertices[vertices.Count - 1],
                friction,
                restitution,
                isSensor,
                collisionGroups);
            tail.Vertex0 = vertices[vertices.Count - 3];
            tail.HasVertex0 = true;

            return new Chain(body, head, tail);
        }

        #endregion

        #region Polygon

        /// <summary>Attaches a polygon fixture to the specified entity.</summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="vertices">The vertices relative to the body's local origin.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> the created fixture is marked as a sensor (i.e. it will fire collision events but the
        ///     collision will not be handled by the solver).
        /// </param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <returns>The created fixture.</returns>
        public static PolygonFixture AttachPolygon(
            this IManager manager,
            Body body,
            IList<LocalPoint> vertices,
            float density = 0,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            if (vertices == null)
            {
                throw new ArgumentNullException("vertices");
            }

            var fixture = manager.AddComponent<PolygonFixture>(body.Entity);
            fixture.Initialize(density, friction, restitution, isSensor, collisionGroups);
            fixture.InitializeShape(vertices);

            // Recompute mass data if necessary.
            if (density > 0)
            {
                body.ResetMassData();
            }

            return fixture;
        }

        /// <summary>Attaches a rectangle fixture to the specified entity.</summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> the created fixture is marked as a sensor (i.e. it will fire collision events but the
        ///     collision will not be handled by the solver).
        /// </param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <returns>The created fixture.</returns>
        public static PolygonFixture AttachRectangle(
            this IManager manager,
            Body body,
            float width,
            float height,
            float density = 0,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            if (width <= 0)
            {
                throw new ArgumentException("Rectangles must have a non-negative, positive width.", "width");
            }
            if (height <= 0)
            {
                throw new ArgumentException("Rectangles must have a non-negative, positive height.", "height");
            }

            // Create the four corner vertices.
            var halfWidth = width / 2;
            var halfHeight = height / 2;
            var vertices = new[]
            {
                new LocalPoint(-halfWidth, -halfHeight),
                new LocalPoint(halfWidth, -halfHeight),
                new LocalPoint(halfWidth, halfHeight),
                new LocalPoint(-halfWidth, halfHeight)
            };

            // Then create a normal polygon.
            return manager.AttachPolygon(body, vertices, density, friction, restitution, isSensor, collisionGroups);
        }

        /// <summary>Attaches a rectangle fixture to the specified entity.</summary>
        /// <param name="manager">The manager the entity resides in.</param>
        /// <param name="body">The body to attach the fixture to.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="localPosition">The offset of the rectangle relative to the body's local origin.</param>
        /// <param name="localAngle">The rotation of the rectangle relative to the body.</param>
        /// <param name="density">The density of the fixture.</param>
        /// <param name="friction">The friction of fixture.</param>
        /// <param name="restitution">The restitution fixture.</param>
        /// <param name="isSensor">
        ///     if set to <c>true</c> the created fixture is marked as a sensor (i.e. it will fire collision events but the
        ///     collision will not be handled by the solver).
        /// </param>
        /// <param name="collisionGroups">The collision groups.</param>
        /// <returns>The created fixture.</returns>
        public static PolygonFixture AttachRectangle(
            this IManager manager,
            Body body,
            float width,
            float height,
            LocalPoint localPosition,
            float localAngle = 0,
            float density = 0,
            float friction = 0.2f,
            float restitution = 0,
            bool isSensor = false,
            uint collisionGroups = 0)
        {
            if (width <= 0)
            {
                throw new ArgumentException("Rectangles must have a non-negative, positive width.", "width");
            }
            if (height <= 0)
            {
                throw new ArgumentException("Rectangles must have a non-negative, positive height.", "height");
            }

            // Create the four corner vertices.
            var halfWidth = width / 2;
            var halfHeight = height / 2;
            var vertices = new[]
            {
                new LocalPoint(-halfWidth, -halfHeight),
                new LocalPoint(halfWidth, - halfHeight),
                new LocalPoint(halfWidth, halfHeight),
                new LocalPoint(-halfWidth, halfHeight)
            };

            // Transform vertices.
            var sin = (float) System.Math.Sin(localAngle);
            var cos = (float) System.Math.Cos(localAngle);
            for (var i = 0; i < vertices.Length; ++i)
            {
                LocalPoint result;
                result.X = (cos * vertices[i].X - sin * vertices[i].Y) + localPosition.X;
                result.Y = (sin * vertices[i].X + cos * vertices[i].Y) + localPosition.Y;
                vertices[i] = result;
            }

            // Then create a normal polygon.
            return manager.AttachPolygon(body, vertices, density, friction, restitution, isSensor, collisionGroups);
        }

        #endregion
    }

    /// <summary>
    ///     This a helper construct to allow dynamic modification of pseudo-chains at some point in time after their
    ///     creation. This is necessary because we do not actually implement Box2D's chain shape but generate actual edges for
    ///     each chain link, instead.
    /// </summary>
    public sealed class Chain
    {
        /// <summary>Gets the body that this chain is attached to.</summary>
        public Body Body
        {
            get { return _body; }
        }

        /// <summary>
        ///     Establish connectivity to a vertex that precedes the first vertex. This may be used to connect an edge or
        ///     other chain shape to this one.
        /// </summary>
        public LocalPoint? PreviousVertex
        {
            get { return _head.HasVertex0 ? (LocalPoint?) _head.Vertex0 : null; }
            set
            {
                if (value != null)
                {
                    _head.Vertex0 = value.Value;
                    _head.HasVertex0 = true;
                }
                else
                {
                    _head.HasVertex0 = false;
                }
            }
        }

        /// <summary>
        ///     Establish connectivity to a vertex that follows the last vertex. This may be used to connect an edge or other
        ///     chain shape to this one.
        /// </summary>
        public LocalPoint? NextVertex
        {
            get { return _tail.HasVertex3 ? (LocalPoint?) _tail.Vertex3 : null; }
            set
            {
                if (value != null)
                {
                    _tail.Vertex3 = value.Value;
                    _tail.HasVertex3 = true;
                }
                else
                {
                    _tail.HasVertex3 = false;
                }
            }
        }

        /// <summary>The body this chain is attached to.</summary>
        private readonly Body _body;

        /// <summary>The head and tail edge fixtures of this chain.</summary>
        private readonly EdgeFixture _head, _tail;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Chain"/> class.
        /// </summary>
        /// <param name="body">The body the chain is attached to.</param>
        /// <param name="head">The head link of the chain.</param>
        /// <param name="tail">The tail link of the chain.</param>
        internal Chain(Body body, EdgeFixture head, EdgeFixture tail)
        {
            System.Diagnostics.Debug.Assert(body != null && head != null && tail != null);
            System.Diagnostics.Debug.Assert(!head.HasVertex0);
            System.Diagnostics.Debug.Assert(!tail.HasVertex3);

            _body = body;
            _head = head;
            _tail = tail;
        }
    }
}