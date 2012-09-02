using System;
using System.Collections.Generic;
using Engine.FarMath;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Common.Decomposition;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.Factories
{
    public static class BodyFactory
    {
        public static Body CreateBody(World world)
        {
            return CreateBody(world, null);
        }

        private static Body CreateBody(World world, object userData)
        {
            Body body = new Body(world, userData);
            return body;
        }

        public static Body CreateBody(World world, FarPosition position)
        {
            return CreateBody(world, position, null);
        }

        private static Body CreateBody(World world, FarPosition position, object userData) 
        {
            Body body = CreateBody(world, userData);
            body.Position = position;
            return body;
        }

        public static Body CreateEdge(World world, FarPosition start, FarPosition end)
        {
            return CreateEdge(world, start, end, null);
        }

        private static Body CreateEdge(World world, FarPosition start, FarPosition end, object userData)
        {
            Body body = CreateBody(world);
            FixtureFactory.AttachEdge((Vector2)start, (Vector2)end, body, userData);
            return body;
        }

        public static Body CreateLoopShape(World world, Vertices vertices)
        {
            return CreateLoopShape(world, vertices, null);
        }

        private static Body CreateLoopShape(World world, Vertices vertices, object userData)
        {
            return CreateLoopShape(world, vertices, FarPosition.Zero, userData);
        }

        public static Body CreateLoopShape(World world, Vertices vertices, FarPosition position)
        {
            return CreateLoopShape(world, vertices, position, null);
        }

        private static Body CreateLoopShape(World world, Vertices vertices, FarPosition position,
                                           object userData)
        {
            Body body = CreateBody(world, position);
            FixtureFactory.AttachLoopShape(vertices, body, userData);
            return body;
        }

        public static Body CreateRectangle(World world, float width, float height, float density)
        {
            return CreateRectangle(world, width, height, density, null);
        }

        private static Body CreateRectangle(World world, float width, float height, float density, object userData)
        {
            return CreateRectangle(world, width, height, density, FarPosition.Zero, userData);
        }

        public static Body CreateRectangle(World world, float width, float height, float density, FarPosition position)
        {
            return CreateRectangle(world, width, height, density, position, null);
        }

        private static Body CreateRectangle(World world, float width, float height, float density, FarPosition position,
                                           object userData)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width", "Width must be more than 0 meters");

            if (height <= 0)
                throw new ArgumentOutOfRangeException("height", "Height must be more than 0 meters");

            Body newBody = CreateBody(world, position);
            Vertices rectangleVertices = PolygonTools.CreateRectangle(width / 2, height / 2);
            PolygonShape rectangleShape = new PolygonShape(rectangleVertices, density);
            newBody.CreateFixture(rectangleShape, userData);

            return newBody;
        }

        public static Body CreateCircle(World world, float radius, float density)
        {
            return CreateCircle(world, radius, density, null);
        }

        private static Body CreateCircle(World world, float radius, float density, object userData)
        {
            return CreateCircle(world, radius, density, FarPosition.Zero, userData);
        }

        public static Body CreateCircle(World world, float radius, float density, FarPosition position)
        {
            return CreateCircle(world, radius, density, position, null);
        }

        private static Body CreateCircle(World world, float radius, float density, FarPosition position, object userData)
        {
            Body body = CreateBody(world, position);
            FixtureFactory.AttachCircle(radius, density, body, userData);
            return body;
        }

        public static Body CreateEllipse(World world, float xRadius, float yRadius, int edges, float density)
        {
            return CreateEllipse(world, xRadius, yRadius, edges, density, null);
        }

        private static Body CreateEllipse(World world, float xRadius, float yRadius, int edges, float density,
                                         object userData)
        {
            return CreateEllipse(world, xRadius, yRadius, edges, density, FarPosition.Zero, userData);
        }

        public static Body CreateEllipse(World world, float xRadius, float yRadius, int edges, float density,
                                         FarPosition position)
        {
            return CreateEllipse(world, xRadius, yRadius, edges, density, position, null);
        }

        private static Body CreateEllipse(World world, float xRadius, float yRadius, int edges, float density,
                                         FarPosition position, object userData)
        {
            Body body = CreateBody(world, position);
            FixtureFactory.AttachEllipse(xRadius, yRadius, edges, density, body, userData);
            return body;
        }

        public static Body CreatePolygon(World world, Vertices vertices, float density)
        {
            return CreatePolygon(world, vertices, density, null);
        }

        private static Body CreatePolygon(World world, Vertices vertices, float density, object userData)
        {
            return CreatePolygon(world, vertices, density, FarPosition.Zero, userData);
        }

        public static Body CreatePolygon(World world, Vertices vertices, float density, FarPosition position)
        {
            return CreatePolygon(world, vertices, density, position, null);
        }

        private static Body CreatePolygon(World world, Vertices vertices, float density, FarPosition position,
                                         object userData)
        {
            Body body = CreateBody(world, position);
            FixtureFactory.AttachPolygon(vertices, density, body, userData);
            return body;
        }

        public static Body CreateCompoundPolygon(World world, List<Vertices> list, float density)
        {
            return CreateCompoundPolygon(world, list, density, BodyType.Static);
        }

        public static Body CreateCompoundPolygon(World world, List<Vertices> list, float density,
                                                 object userData)
        {
            return CreateCompoundPolygon(world, list, density, FarPosition.Zero, userData);
        }

        public static Body CreateCompoundPolygon(World world, List<Vertices> list, float density,
                                                 FarPosition position)
        {
            return CreateCompoundPolygon(world, list, density, position, null);
        }

        private static Body CreateCompoundPolygon(World world, List<Vertices> list, float density,
                                                 FarPosition position, object userData)
        {
            //We create a single body
            Body polygonBody = CreateBody(world, position);
            FixtureFactory.AttachCompoundPolygon(list, density, polygonBody, userData);
            return polygonBody;
        }


        public static Body CreateGear(World world, float radius, int numberOfTeeth, float tipPercentage,
                                      float toothHeight, float density)
        {
            return CreateGear(world, radius, numberOfTeeth, tipPercentage, toothHeight, density, null);
        }

        private static Body CreateGear(World world, float radius, int numberOfTeeth, float tipPercentage,
                                      float toothHeight, float density, object userData)
        {
            Vertices gearPolygon = PolygonTools.CreateGear(radius, numberOfTeeth, tipPercentage, toothHeight);

            //Gears can in some cases be convex
            if (!gearPolygon.IsConvex())
            {
                //Decompose the gear:
                List<Vertices> list = EarclipDecomposer.ConvexPartition(gearPolygon);

                return CreateCompoundPolygon(world, list, density, userData);
            }

            return CreatePolygon(world, gearPolygon, density, userData);
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
        /// <returns></returns>
        private static Body CreateCapsule(World world, float height, float topRadius, int topEdges,
                                         float bottomRadius,
                                         int bottomEdges, float density, FarPosition position, object userData)
        {
            Vertices verts = PolygonTools.CreateCapsule(height, topRadius, topEdges, bottomRadius, bottomEdges);

            Body body;

            //There are too many vertices in the capsule. We decompose it.
            if (verts.Count >= Settings.MaxPolygonVertices)
            {
                List<Vertices> vertList = EarclipDecomposer.ConvexPartition(verts);
                body = CreateCompoundPolygon(world, vertList, density, userData);
                body.Position = position;

                return body;
            }

            body = CreatePolygon(world, verts, density, userData);
            body.Position = position;

            return body;
        }

        public static Body CreateCapsule(World world, float height, float topRadius, int topEdges,
                                         float bottomRadius,
                                         int bottomEdges, float density, FarPosition position)
        {
            return CreateCapsule(world, height, topRadius, topEdges, bottomRadius, bottomEdges, density, position, null);
        }

        public static Body CreateCapsule(World world, float height, float endRadius, float density)
        {
            return CreateCapsule(world, height, endRadius, density, null);
        }

        private static Body CreateCapsule(World world, float height, float endRadius, float density,
                                         object userData)
        {
            //Create the middle rectangle
            Vertices rectangle = PolygonTools.CreateRectangle(endRadius, height / 2);

            List<Vertices> list = new List<Vertices>();
            list.Add(rectangle);

            Body body = CreateCompoundPolygon(world, list, density, userData);

            //Create the two circles
            CircleShape topCircle = new CircleShape(endRadius, density);
            topCircle.Position = new Vector2(0, height / 2);
            body.CreateFixture(topCircle, userData);

            CircleShape bottomCircle = new CircleShape(endRadius, density);
            bottomCircle.Position = new Vector2(0, -(height / 2));
            body.CreateFixture(bottomCircle, userData);
            return body;
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
        /// <returns></returns>
        private static Body CreateRoundedRectangle(World world, float width, float height, float xRadius,
                                                  float yRadius,
                                                  int segments, float density, FarPosition position,
                                                  object userData)
        {
            Vertices verts = PolygonTools.CreateRoundedRectangle(width, height, xRadius, yRadius, segments);

            //There are too many vertices in the capsule. We decompose it.
            if (verts.Count >= Settings.MaxPolygonVertices)
            {
                List<Vertices> vertList = EarclipDecomposer.ConvexPartition(verts);
                Body body = CreateCompoundPolygon(world, vertList, density, userData);
                body.Position = position;
                return body;
            }

            return CreatePolygon(world, verts, density);
        }

        public static Body CreateRoundedRectangle(World world, float width, float height, float xRadius,
                                                  float yRadius,
                                                  int segments, float density, FarPosition position)
        {
            return CreateRoundedRectangle(world, width, height, xRadius, yRadius, segments, density, position, null);
        }

        public static Body CreateRoundedRectangle(World world, float width, float height, float xRadius,
                                                  float yRadius,
                                                  int segments, float density)
        {
            return CreateRoundedRectangle(world, width, height, xRadius, yRadius, segments, density, null);
        }

        private static Body CreateRoundedRectangle(World world, float width, float height, float xRadius,
                                                  float yRadius,
                                                  int segments, float density, object userData)
        {
            return CreateRoundedRectangle(world, width, height, xRadius, yRadius, segments, density, FarPosition.Zero,
                                          userData);
        }

        public static BreakableBody CreateBreakableBody(World world, Vertices vertices, float density)
        {
            return CreateBreakableBody(world, vertices, density, null);
        }

        private static BreakableBody CreateBreakableBody(World world, Vertices vertices, float density, object userData)
        {
            return CreateBreakableBody(world, vertices, density, FarPosition.Zero, userData);
        }

        /// <summary>
        /// Creates a breakable body. You would want to remove collinear points before using this.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="density">The density.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        private static BreakableBody CreateBreakableBody(World world, Vertices vertices, float density, FarPosition position,
                                                        object userData)
        {
            List<Vertices> triangles = EarclipDecomposer.ConvexPartition(vertices);

            BreakableBody breakableBody = new BreakableBody(triangles, world, density, userData);
            breakableBody.MainBody.Position = position;
            world.AddBreakableBody(breakableBody);

            return breakableBody;
        }

        public static BreakableBody CreateBreakableBody(World world, Vertices vertices, float density, FarPosition position)
        {
            return CreateBreakableBody(world, vertices, density, position, null);
        }

        public static Body CreateLineArc(World world, float radians, int sides, float radius, Vector2 position,
                                         float angle, bool closed)
        {
            Body body = CreateBody(world);
            FixtureFactory.AttachLineArc(radians, sides, radius, position, angle, closed, body);
            return body;
        }

        public static Body CreateSolidArc(World world, float density, float radians, int sides, float radius,
                                          Vector2 position, float angle)
        {
            Body body = CreateBody(world);
            FixtureFactory.AttachSolidArc(density, radians, sides, radius, position, angle, body);
            return body;
        }
    }
}