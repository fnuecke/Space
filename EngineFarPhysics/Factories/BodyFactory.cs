using System;
using System.Collections.Generic;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Common.Decomposition;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using WorldVector2 = Engine.FarMath.FarPosition;

namespace FarseerPhysics.Factories
{
    /// <summary>
    /// Factory for easier body creation.
    /// </summary>
    public static class BodyFactory
    {
        /// <summary>
        /// Creates the body.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateBody(World world, object userData = null)
        {
            return new Body(world, userData);
        }

        /// <summary>
        /// Creates the body.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public static Body CreateBody(World world, Vector2 position)
        {
            return CreateBody(world, (WorldVector2)position);
        }

        /// <summary>
        /// Creates the body.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateBody(World world, Vector2 position, object userData)
        {
            return CreateBody(world, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates the body.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateBody(World world, WorldVector2 position, object userData = null)
        {
            var body = CreateBody(world, userData);
            body.Position = position;
            return body;
        }

        /// <summary>
        /// Creates the edge.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns></returns>
        public static Body CreateEdge(World world, Vector2 start, Vector2 end)
        {
            return CreateEdge(world, (WorldVector2)start, (WorldVector2)end);
        }

        /// <summary>
        /// Creates the edge.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateEdge(World world, Vector2 start, Vector2 end, object userData)
        {
            return CreateEdge(world, (WorldVector2)start, (WorldVector2)end, userData);
        }

        /// <summary>
        /// Creates the edge.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateEdge(World world, WorldVector2 start, WorldVector2 end, object userData = null)
        {
            var mid = start + (Vector2)(end - start) * 0.5f;
            var body = CreateBody(world, mid);
            FixtureFactory.AttachEdge((Vector2)(start - mid), (Vector2)(end - mid), body, userData);
            return body;
        }

        /// <summary>
        /// Creates the loop shape.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateLoopShape(World world, Vertices vertices, object userData = null)
        {
            return CreateLoopShape(world, vertices, WorldVector2.Zero, userData);
        }

        /// <summary>
        /// Creates the loop shape.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public static Body CreateLoopShape(World world, Vertices vertices, Vector2 position)
        {
            return CreateLoopShape(world, vertices, (WorldVector2)position);
        }

        /// <summary>
        /// Creates the loop shape.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateLoopShape(World world, Vertices vertices, Vector2 position, object userData)
        {
            return CreateLoopShape(world, vertices, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates the loop shape.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateLoopShape(World world, Vertices vertices, WorldVector2 position, object userData = null)
        {
            var body = CreateBody(world, position);
            FixtureFactory.AttachLoopShape(vertices, body, userData);
            return body;
        }

        /// <summary>
        /// Creates the rectangle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="density">The density.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateRectangle(World world, float width, float height, float density, object userData = null)
        {
            return CreateRectangle(world, width, height, density, WorldVector2.Zero, userData);
        }

        /// <summary>
        /// Creates the rectangle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public static Body CreateRectangle(World world, float width, float height, float density, Vector2 position)
        {
            return CreateRectangle(world, width, height, density, (WorldVector2)position);
        }

        /// <summary>
        /// Creates the rectangle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateRectangle(World world, float width, float height, float density, Vector2 position, object userData)
        {
            return CreateRectangle(world, width, height, density, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates the rectangle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateRectangle(World world, float width, float height, float density, WorldVector2 position, object userData = null)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException("width", "Width must be more than 0 meters");
            }
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException("height", "Height must be more than 0 meters");
            }

            var newBody = CreateBody(world, position);
            newBody.CreateFixture(new PolygonShape(PolygonTools.CreateRectangle(width / 2, height / 2), density), userData);
            return newBody;
        }

        /// <summary>
        /// Creates the circle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="density">The density.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateCircle(World world, float radius, float density, object userData = null)
        {
            return CreateCircle(world, radius, density, WorldVector2.Zero, userData);
        }

        /// <summary>
        /// Creates the circle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public static Body CreateCircle(World world, float radius, float density, Vector2 position)
        {
            return CreateCircle(world, radius, density, (WorldVector2)position);
        }

        /// <summary>
        /// Creates the circle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateCircle(World world, float radius, float density, Vector2 position, object userData)
        {
            return CreateCircle(world, radius, density, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates the circle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateCircle(World world, float radius, float density, WorldVector2 position, object userData = null)
        {
            var body = CreateBody(world, position);
            FixtureFactory.AttachCircle(radius, density, body, userData);
            return body;
        }

        /// <summary>
        /// Creates the ellipse.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="xRadius">The x radius.</param>
        /// <param name="yRadius">The y radius.</param>
        /// <param name="edges">The edges.</param>
        /// <param name="density">The density.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateEllipse(World world, float xRadius, float yRadius, int edges, float density, object userData = null)
        {
            return CreateEllipse(world, xRadius, yRadius, edges, density, WorldVector2.Zero, userData);
        }

        /// <summary>
        /// Creates the ellipse.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="xRadius">The x radius.</param>
        /// <param name="yRadius">The y radius.</param>
        /// <param name="edges">The edges.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public static Body CreateEllipse(World world, float xRadius, float yRadius, int edges, float density, Vector2 position)
        {
            return CreateEllipse(world, xRadius, yRadius, edges, density, (WorldVector2)position);
        }

        /// <summary>
        /// Creates the ellipse.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="xRadius">The x radius.</param>
        /// <param name="yRadius">The y radius.</param>
        /// <param name="edges">The edges.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateEllipse(World world, float xRadius, float yRadius, int edges, float density, Vector2 position, object userData)
        {
            return CreateEllipse(world, xRadius, yRadius, edges, density, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates the ellipse.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="xRadius">The x radius.</param>
        /// <param name="yRadius">The y radius.</param>
        /// <param name="edges">The edges.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateEllipse(World world, float xRadius, float yRadius, int edges, float density, WorldVector2 position, object userData = null)
        {
            var body = CreateBody(world, position);
            FixtureFactory.AttachEllipse(xRadius, yRadius, edges, density, body, userData);
            return body;
        }

        /// <summary>
        /// Creates the polygon.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="density">The density.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreatePolygon(World world, Vertices vertices, float density, object userData = null)
        {
            return CreatePolygon(world, vertices, density, WorldVector2.Zero, userData);
        }

        /// <summary>
        /// Creates the polygon.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public static Body CreatePolygon(World world, Vertices vertices, float density, Vector2 position)
        {
            return CreatePolygon(world, vertices, density, (WorldVector2)position);
        }

        /// <summary>
        /// Creates the polygon.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreatePolygon(World world, Vertices vertices, float density, Vector2 position, object userData)
        {
            return CreatePolygon(world, vertices, density, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates the polygon.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreatePolygon(World world, Vertices vertices, float density, WorldVector2 position, object userData = null)
        {
            var body = CreateBody(world, position);
            FixtureFactory.AttachPolygon(vertices, density, body, userData);
            return body;
        }

        /// <summary>
        /// Creates the compound polygon.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="list">The list.</param>
        /// <param name="density">The density.</param>
        /// <returns></returns>
        public static Body CreateCompoundPolygon(World world, List<Vertices> list, float density)
        {
            return CreateCompoundPolygon(world, list, density, BodyType.Static);
        }

        /// <summary>
        /// Creates the compound polygon.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="list">The list.</param>
        /// <param name="density">The density.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateCompoundPolygon(World world, List<Vertices> list, float density, object userData)
        {
            return CreateCompoundPolygon(world, list, density, WorldVector2.Zero, userData);
        }

        /// <summary>
        /// Creates the compound polygon.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="list">The list.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateCompoundPolygon(World world, List<Vertices> list, float density, Vector2 position, object userData = null)
        {
            return CreateCompoundPolygon(world, list, density, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates the compound polygon.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="list">The list.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateCompoundPolygon(World world, List<Vertices> list, float density, WorldVector2 position, object userData = null)
        {
            var polygonBody = CreateBody(world, position);
            FixtureFactory.AttachCompoundPolygon(list, density, polygonBody, userData);
            return polygonBody;
        }

        /// <summary>
        /// Creates the gear.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="numberOfTeeth">The number of teeth.</param>
        /// <param name="tipPercentage">The tip percentage.</param>
        /// <param name="toothHeight">Height of the tooth.</param>
        /// <param name="density">The density.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateGear(World world, float radius, int numberOfTeeth, float tipPercentage, float toothHeight, float density, object userData = null)
        {
            var gearPolygon = PolygonTools.CreateGear(radius, numberOfTeeth, tipPercentage, toothHeight);

            // Gears can in some cases be convex.
            if (!gearPolygon.IsConvex())
            {
                // Decompose the gear.
                return CreateCompoundPolygon(world, EarclipDecomposer.ConvexPartition(gearPolygon), density, userData);
            }

            return CreatePolygon(world, gearPolygon, density, userData);
        }

        /// <summary>
        /// Creates the capsule.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="height">The height.</param>
        /// <param name="topRadius">The top radius.</param>
        /// <param name="topEdges">The top edges.</param>
        /// <param name="bottomRadius">The bottom radius.</param>
        /// <param name="bottomEdges">The bottom edges.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateCapsule(World world, float height, float topRadius, int topEdges, float bottomRadius, int bottomEdges, float density, Vector2 position, object userData = null)
        {
            return CreateCapsule(world, height, topRadius, topEdges, bottomRadius, bottomEdges, density, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates a capsule.
        /// Note: Automatically decomposes the capsule if it contains too many vertices (controlled by Settings.MaxPolygonVertices)
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="height">The height.</param>
        /// <param name="topRadius">The top radius.</param>
        /// <param name="topEdges">The top edges.</param>
        /// <param name="bottomRadius">The bottom radius.</param>
        /// <param name="bottomEdges">The bottom edges.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateCapsule(World world, float height, float topRadius, int topEdges, float bottomRadius, int bottomEdges, float density, WorldVector2 position, object userData = null)
        {
            var vertices = PolygonTools.CreateCapsule(height, topRadius, topEdges, bottomRadius, bottomEdges);

            // If there are too many vertices in the capsule. We decompose it.
            if (vertices.Count >= Settings.MaxPolygonVertices)
            {
                var body = CreateCompoundPolygon(world, EarclipDecomposer.ConvexPartition(vertices), density, userData);
                body.Position = position;
                return body;
            }
            else
            {
                var body = CreatePolygon(world, vertices, density, userData);
                body.Position = position;
                return body;
            }
        }

        /// <summary>
        /// Creates the capsule.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="height">The height.</param>
        /// <param name="endRadius">The end radius.</param>
        /// <param name="density">The density.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateCapsule(World world, float height, float endRadius, float density, object userData = null)
        {
            // Create the middle rectangle.
            var body = CreateRectangle(world, endRadius * 2, height, density, userData);
            // Create the two circles.
            body.CreateFixture(new CircleShape(endRadius, density) {Position = new Vector2(0, height / 2)}, userData);
            body.CreateFixture(new CircleShape(endRadius, density) {Position = new Vector2(0, -(height / 2))}, userData);
            return body;
        }

        /// <summary>
        /// Creates the rounded rectangle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="xRadius">The x radius.</param>
        /// <param name="yRadius">The y radius.</param>
        /// <param name="segments">The segments.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateRoundedRectangle(World world, float width, float height, float xRadius, float yRadius, int segments, float density, Vector2 position, object userData = null)
        {
            return CreateRoundedRectangle(world, width, height, xRadius, yRadius, segments, density, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates a rounded rectangle.
        /// Note: Automatically decomposes the capsule if it contains too many vertices (controlled by Settings.MaxPolygonVertices)
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="xRadius">The x radius.</param>
        /// <param name="yRadius">The y radius.</param>
        /// <param name="segments">The segments.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateRoundedRectangle(World world, float width, float height, float xRadius, float yRadius, int segments, float density, WorldVector2 position, object userData = null)
        {
            var vertices = PolygonTools.CreateRoundedRectangle(width, height, xRadius, yRadius, segments);

            //There are too many vertices in the capsule. We decompose it.
            if (vertices.Count >= Settings.MaxPolygonVertices)
            {
                var body = CreateCompoundPolygon(world, EarclipDecomposer.ConvexPartition(vertices), density, userData);
                body.Position = position;
                return body;
            }
            else
            {
                return CreatePolygon(world, vertices, density);
            }
        }

        /// <summary>
        /// Creates the rounded rectangle.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="xRadius">The x radius.</param>
        /// <param name="yRadius">The y radius.</param>
        /// <param name="segments">The segments.</param>
        /// <param name="density">The density.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Body CreateRoundedRectangle(World world, float width, float height, float xRadius, float yRadius, int segments, float density, object userData = null)
        {
            return CreateRoundedRectangle(world, width, height, xRadius, yRadius, segments, density, WorldVector2.Zero, userData);
        }

        /// <summary>
        /// Creates the line arc.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="radians">The radians.</param>
        /// <param name="sides">The sides.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="offset">The position.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="closed">if set to <c>true</c> [closed].</param>
        /// <returns></returns>
        public static Body CreateLineArc(World world, float radians, int sides, float radius, Vector2 offset, float angle, bool closed)
        {
            var body = CreateBody(world);
            FixtureFactory.AttachLineArc(radians, sides, radius, offset, angle, closed, body);
            return body;
        }

        /// <summary>
        /// Creates the solid arc.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="density">The density.</param>
        /// <param name="radians">The radians.</param>
        /// <param name="sides">The sides.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="offset">The position.</param>
        /// <param name="angle">The angle.</param>
        /// <returns></returns>
        public static Body CreateSolidArc(World world, float density, float radians, int sides, float radius, Vector2 offset, float angle)
        {
            var body = CreateBody(world);
            FixtureFactory.AttachSolidArc(density, radians, sides, radius, offset, angle, body);
            return body;
        }

#if BREAKABLES
    /// <summary>
    /// Creates the breakable body.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="vertices">The vertices.</param>
    /// <param name="density">The density.</param>
    /// <param name="userData">The user data.</param>
    /// <returns></returns>
        public static BreakableBody CreateBreakableBody(World world, Vertices vertices, float density, object userData = null)
        {
            return CreateBreakableBody(world, vertices, density, WorldVector2.Zero, userData);
        }

        /// <summary>
        /// Creates the breakable body.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static BreakableBody CreateBreakableBody(World world, Vertices vertices, float density, Vector2 position, object userData = null)
        {
            return CreateBreakableBody(world, vertices, density, (WorldVector2)position, userData);
        }

        /// <summary>
        /// Creates a breakable body. You would want to remove collinear points before using this.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static BreakableBody CreateBreakableBody(World world, Vertices vertices, float density, WorldVector2 position, object userData = null)
        {
            var breakableBody = new BreakableBody(EarclipDecomposer.ConvexPartition(vertices), world, density, userData);
            breakableBody.MainBody.Position = position;
            world.AddBreakableBody(breakableBody);
            return breakableBody;
        }
#endif
    }
}