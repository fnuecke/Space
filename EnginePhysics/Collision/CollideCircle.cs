using Engine.Physics.Components;
using Engine.Physics.Math;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Collision
{
    /// <summary>
    /// Methods for circle vs. circle and circle vs. polygon collisions.
    /// </summary>
    internal static partial class Algorithms
    {
        public static bool CollideCircles(Fixture fixtureA, WorldTransform xfA,
                                          Fixture fixtureB, WorldTransform xfB,
                                          out Manifold manifold)
        {
            manifold = new Manifold();

            var circleA = fixtureA as CircleFixture;
            var circleB = fixtureB as CircleFixture;

            System.Diagnostics.Debug.Assert(circleA != null);
            System.Diagnostics.Debug.Assert(circleB != null);

            var pA = xfA.ToGlobal(circleA.Center);
            var pB = xfB.ToGlobal(circleB.Center);

            var d = (Vector2)(pB - pA);
            var distSqr = Vector2.Dot(d, d);
            var radius = circleA.Radius + circleB.Radius;
            if (distSqr > radius * radius)
            {
                return false;
            }

            manifold.Type = Manifold.ManifoldType.Circles;
            manifold.LocalPoint = circleA.Center;
            manifold.LocalNormal = Vector2.Zero;
            manifold.PointCount = 1;
            manifold.Points.Item1.LocalPoint = circleB.Center;
            manifold.Points.Item1.Id.Key = 0;

            return true;
        }

        public static bool CollidePolygonAndCircle(Fixture fixtureA, WorldTransform xfA,
                                                   Fixture fixtureB, WorldTransform xfB,
                                                   out Manifold manifold)
        {
            manifold = new Manifold();

            var polygonA = fixtureA as PolygonFixture;
            var circleB = fixtureB as CircleFixture;

            System.Diagnostics.Debug.Assert(polygonA != null);
            System.Diagnostics.Debug.Assert(circleB != null);

            // Compute circle position in the frame of the polygon.
            var centerInA = xfA.ToLocal(xfB.ToGlobal(circleB.Center));

            var totalRadius = polygonA.Radius + circleB.Radius;
            var vertexCountA = polygonA.Count;
            var verticesA = polygonA.Vertices;
            var normalsA = polygonA.Normals;

            // Find the min separating edge.
            var normalIndexA = 0;
            var separation = float.MinValue;
            for (var i = 0; i < vertexCountA; ++i)
            {
                var s = Vector2.Dot(normalsA[i], centerInA - verticesA[i]);

                if (s > totalRadius)
                {
                    // Early out.
                    manifold = new Manifold();
                    return false;
                }

                if (s > separation)
                {
                    separation = s;
                    normalIndexA = i;
                }
            }

            // Vertices that subtend the incident face.
            var vertexIndexA1 = normalIndexA;
            var vertexIndexA2 = vertexIndexA1 + 1 < vertexCountA ? vertexIndexA1 + 1 : 0;
            var vertexA1 = verticesA[vertexIndexA1];
            var vertexA2 = verticesA[vertexIndexA2];

            // If the center is inside the polygon ...
            if (separation < Settings.Epsilon)
            {
                manifold.Type = Manifold.ManifoldType.FaceA;
                manifold.PointCount = 1;
                manifold.Points.Item1.LocalPoint = circleB.Center;
                manifold.Points.Item1.Id.Key = 0;

                manifold.LocalNormal = normalsA[normalIndexA];
                manifold.LocalPoint = 0.5f * (vertexA1 + vertexA2);
                return true;
            }

            // Compute barycentric coordinates
            var u1 = Vector2.Dot(centerInA - vertexA1, vertexA2 - vertexA1);
            var u2 = Vector2.Dot(centerInA - vertexA2, vertexA1 - vertexA2);

            if (u1 <= 0.0f)
            {
                if (LocalPoint.DistanceSquared(centerInA, vertexA1) <= totalRadius * totalRadius)
                {

                    var normalInA = centerInA - vertexA1;
                    normalInA.Normalize();

                    manifold.Type = Manifold.ManifoldType.FaceA;
                    manifold.PointCount = 1;
                    manifold.Points.Item1.LocalPoint = circleB.Center;
                    manifold.Points.Item1.Id.Key = 0;

                    manifold.LocalNormal = normalInA;
                    manifold.LocalPoint = vertexA1;
                    return true;
                }
            }
            else if (u2 <= 0.0f)
            {
                if (LocalPoint.DistanceSquared(centerInA, vertexA2) <= totalRadius * totalRadius)
                {
                    manifold.Type = Manifold.ManifoldType.FaceA;
                    manifold.PointCount = 1;
                    manifold.Points.Item1.LocalPoint = circleB.Center;
                    manifold.Points.Item1.Id.Key = 0;

                    manifold.LocalNormal = centerInA - vertexA2;
                    manifold.LocalNormal.Normalize();
                    manifold.LocalPoint = vertexA2;
                    return true;
                }
            }
            else
            {
                var faceCenter = 0.5f * (vertexA1 + vertexA2);
                if (Vector2.Dot(centerInA - faceCenter, normalsA[vertexIndexA1]) <= totalRadius)
                {
                    manifold.Type = Manifold.ManifoldType.FaceA;
                    manifold.PointCount = 1;
                    manifold.Points.Item1.LocalPoint = circleB.Center;
                    manifold.Points.Item1.Id.Key = 0;

                    manifold.LocalNormal = normalsA[vertexIndexA1];
                    manifold.LocalPoint = faceCenter;
                    return true;
                }
            }

            return false;
        }
    }
}
