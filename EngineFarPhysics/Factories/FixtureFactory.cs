using System;
using System.Collections.Generic;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Common.Decomposition;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.Factories
{
    /// <summary>
    /// An easy to use factory for creating fixtures.
    /// </summary>
    public static class FixtureFactory
    {
        /// <summary>
        /// Attaches the edge.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="body">The body.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Fixture AttachEdge(Vector2 start, Vector2 end, Body body, object userData = null)
        {
            return body.CreateFixture(new EdgeShape(start, end), userData);
        }

        /// <summary>
        /// Attaches the loop shape.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="body">The body.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Fixture AttachLoopShape(Vertices vertices, Body body, object userData = null)
        {
            return body.CreateFixture(new LoopShape(vertices), userData);
        }

        /// <summary>
        /// Attaches the rectangle.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="density">The density.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="body">The body.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Fixture AttachRectangle(float width, float height, float density, Vector2 offset, Body body, object userData = null)
        {
            var rectangleVertices = PolygonTools.CreateRectangle(width / 2, height / 2);
            rectangleVertices.Translate(ref offset);
            return body.CreateFixture(new PolygonShape(rectangleVertices, density), userData);
        }

        /// <summary>
        /// Attaches the circle.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="density">The density.</param>
        /// <param name="body">The body.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Fixture AttachCircle(float radius, float density, Body body, object userData = null)
        {
            if (radius <= 0)
            {
                throw new ArgumentOutOfRangeException("radius", "Radius must be more than 0 meters");
            }

            return body.CreateFixture(new CircleShape(radius, density), userData);
        }

        /// <summary>
        /// Attaches the circle.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="density">The density.</param>
        /// <param name="body">The body.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Fixture AttachCircle(float radius, float density, Body body, Vector2 offset, object userData = null)
        {
            if (radius <= 0)
            {
                throw new ArgumentOutOfRangeException("radius", "Radius must be more than 0 meters");
            }

            return body.CreateFixture(new CircleShape(radius, density) {Position = offset}, userData);
        }

        /// <summary>
        /// Attaches the polygon.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="density">The density.</param>
        /// <param name="body">The body.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Fixture AttachPolygon(Vertices vertices, float density, Body body, object userData = null)
        {
            if (vertices.Count <= 1)
            {
                throw new ArgumentOutOfRangeException("vertices", "Too few points to be a polygon");
            }

            return body.CreateFixture(new PolygonShape(vertices, density), userData);
        }

        /// <summary>
        /// Attaches the ellipse.
        /// </summary>
        /// <param name="xRadius">The x radius.</param>
        /// <param name="yRadius">The y radius.</param>
        /// <param name="edges">The edges.</param>
        /// <param name="density">The density.</param>
        /// <param name="body">The body.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static Fixture AttachEllipse(float xRadius, float yRadius, int edges, float density, Body body,
                                            object userData = null)
        {
            if (xRadius <= 0)
            {
                throw new ArgumentOutOfRangeException("xRadius", "X-radius must be more than 0");
            }

            if (yRadius <= 0)
            {
                throw new ArgumentOutOfRangeException("yRadius", "Y-radius must be more than 0");
            }

            return body.CreateFixture(new PolygonShape(PolygonTools.CreateEllipse(xRadius, yRadius, edges), density), userData);
        }

        /// <summary>
        /// Attaches the compound polygon.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="density">The density.</param>
        /// <param name="body">The body.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public static List<Fixture> AttachCompoundPolygon(List<Vertices> list, float density, Body body, object userData = null)
        {
            var result = new List<Fixture>(list.Count);

            // Then we create several fixtures using the body.
            foreach (var vertices in list)
            {
                result.Add(body.CreateFixture(vertices.Count == 2
                               ? (Shape)new EdgeShape(vertices[0], vertices[1])
                               : (Shape)new PolygonShape(vertices, density), userData));
            }

            return result;
        }

        /// <summary>
        /// Attaches the line arc.
        /// </summary>
        /// <param name="radians">The radians.</param>
        /// <param name="sides">The sides.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="position">The position.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="closed">if set to <c>true</c> [closed].</param>
        /// <param name="body">The body.</param>
        /// <returns></returns>
        public static List<Fixture> AttachLineArc(float radians, int sides, float radius, Vector2 position, float angle, bool closed, Body body)
        {
            var arc = PolygonTools.CreateArc(radians, sides, radius);
            arc.Rotate((MathHelper.Pi - radians) / 2 + angle);
            arc.Translate(ref position);

            var result = new List<Fixture>(arc.Count);

            if (closed)
            {
                result.Add(AttachLoopShape(arc, body));
            }

            for (var i = 1; i < arc.Count; i++)
            {
                result.Add(AttachEdge(arc[i], arc[i - 1], body));
            }

            return result;
        }

        /// <summary>
        /// Attaches the solid arc.
        /// </summary>
        /// <param name="density">The density.</param>
        /// <param name="radians">The radians.</param>
        /// <param name="sides">The sides.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="position">The position.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="body">The body.</param>
        /// <returns></returns>
        public static List<Fixture> AttachSolidArc(float density, float radians, int sides, float radius, Vector2 position, float angle, Body body)
        {
            var arc = PolygonTools.CreateArc(radians, sides, radius);
            arc.Rotate((MathHelper.Pi - radians) / 2 + angle);
            arc.Translate(ref position);

            //Close the arc
            arc.Add(arc[0]);

            return AttachCompoundPolygon(EarclipDecomposer.ConvexPartition(arc), density, body);
        }
    }
}