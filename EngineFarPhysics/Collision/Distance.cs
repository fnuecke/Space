/*
* Farseer Physics Engine based on Box2D.XNA port:
* Copyright (c) 2010 Ian Qvist
* 
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.box2d.org 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using System.Diagnostics;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using Microsoft.Xna.Framework;
using WorldVector2 = Engine.FarMath.FarPosition;

namespace FarseerPhysics.Collision
{
    /// <summary>
    /// A distance proxy is used by the GJK algorithm.
    /// It encapsulates any shape.
    /// </summary>
    internal sealed class DistanceProxy
    {
        public float Radius;

        public readonly Vertices Vertices = new Vertices();

        /// <summary>
        /// Initialize the proxy using the given shape. The shape
        /// must remain in scope while the proxy is in use.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="index">The index.</param>
        public void Set(Shape shape, int index)
        {
            switch (shape.ShapeType)
            {
                case ShapeType.Circle:
                {
                    var circle = (CircleShape)shape;
                    Vertices.Clear();
                    Vertices.Add(circle.Position);
                    Radius = circle.Radius;
                    break;
                }

                case ShapeType.Polygon:
                {
                    var polygon = (PolygonShape)shape;
                    Vertices.Clear();
                    for (var i = 0; i < polygon.Vertices.Count; i++)
                    {
                        Vertices.Add(polygon.Vertices[i]);
                    }
                    Radius = polygon.Radius;
                    break;
                }

                case ShapeType.Loop:
                {
                    var loop = (LoopShape)shape;
                    Debug.Assert(0 <= index && index < loop.Vertices.Count);
                    Vertices.Clear();
                    Vertices.Add(loop.Vertices[index]);
                    Vertices.Add(index + 1 < loop.Vertices.Count ? loop.Vertices[index + 1] : loop.Vertices[0]);

                    Radius = loop.Radius;
                    break;
                }

                case ShapeType.Edge:
                {
                    var edge = (EdgeShape)shape;
                    Vertices.Clear();
                    Vertices.Add(edge.Vertex1);
                    Vertices.Add(edge.Vertex2);
                    Radius = edge.Radius;
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Get the supporting vertex index in the given direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public int GetSupport(Vector2 direction)
        {
            var bestIndex = 0;
            var bestValue = Vector2.Dot(Vertices[0], direction);
            for (int i = 1, count = Vertices.Count; i < count; ++i)
            {
                var value = Vector2.Dot(Vertices[i], direction);
                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;
                }
            }
            return bestIndex;
        }
    }

    /// <summary>
    /// Input for ComputeDistance.
    /// You have to option to use the shape radii
    /// in the computation. 
    /// </summary>
    internal sealed class DistanceInput
    {
        public DistanceProxy ProxyA = new DistanceProxy();

        public DistanceProxy ProxyB = new DistanceProxy();

        public Transform TransformA;

        public Transform TransformB;

        public bool UseRadii;
    }

    /// <summary>
    /// Output for ComputeDistance.
    /// </summary>
    internal struct DistanceOutput
    {
        /// <summary>
        /// Minimal distance between the two shapes.
        /// </summary>
        public float Distance;

        /// <summary>
        /// Closest point on shapeA.
        /// </summary>
        public WorldVector2 PointA;

        /// <summary>
        /// Closest point on shapeB.
        /// </summary>
        public WorldVector2 PointB;
    }

    /// <summary>
    /// Used to warm start ComputeDistance.
    /// Set count to zero on first call.
    /// </summary>
    internal struct SimplexCache
    {
        /// <summary>
        /// Number of actually involved points.
        /// </summary>
        public ushort Count;

        /// <summary>
        /// Vertex indexes on shape A.
        /// </summary>
        public FixedArray3<byte> IndexA;

        /// <summary>
        /// Vertex indexes on shape B.
        /// </summary>
        public FixedArray3<byte> IndexB;

        public float Metric;
    }

    internal struct SimplexVertex
    {
        /// <summary>
        /// Barycentric coordinate for closest point 
        /// </summary>
        public float A;

        /// <summary>
        /// wA index
        /// </summary>
        public int IndexA;

        /// <summary>
        /// wB index
        /// </summary>
        public int IndexB;

        /// <summary>
        /// wB - wA
        /// </summary>
        public Vector2 W;

        /// <summary>
        /// Support point in proxyA
        /// </summary>
        public WorldVector2 WA;

        /// <summary>
        /// Support point in proxyB
        /// </summary>
        public WorldVector2 WB;
    }

    internal struct Simplex
    {
        internal int Count;
        internal FixedArray3<SimplexVertex> V;

        internal void ReadCache(ref SimplexCache cache,
                                DistanceProxy proxyA, ref Transform transformA,
                                DistanceProxy proxyB, ref Transform transformB)
        {
            Debug.Assert(cache.Count <= 3);

            // Copy data from cache.
            Count = cache.Count;
            for (var i = 0; i < Count; ++i)
            {
                var v = V[i];
                v.IndexA = cache.IndexA[i];
                v.IndexB = cache.IndexB[i];
                var wALocal = proxyA.Vertices[v.IndexA];
                var wBLocal = proxyB.Vertices[v.IndexB];
                v.WA = MathUtils.Multiply(ref transformA, wALocal);
                v.WB = MathUtils.Multiply(ref transformB, wBLocal);
                v.W = (Vector2)(v.WB - v.WA);
                v.A = 0.0f;
                V[i] = v;
            }

            // Compute the new simplex metric, if it is substantially different than
            // old metric then flush the simplex.
            if (Count > 1)
            {
                var metric1 = cache.Metric;
                var metric2 = GetMetric();
                if (metric2 < 0.5f * metric1 || 2.0f * metric1 < metric2 || metric2 < Settings.Epsilon)
                {
                    // Reset the simplex.
                    Count = 0;
                }
            }

            // If the cache is empty or invalid ...
            if (Count == 0)
            {
                var v = V[0];
                v.IndexA = 0;
                v.IndexB = 0;
                var wALocal = proxyA.Vertices[0];
                var wBLocal = proxyB.Vertices[0];
                v.WA = MathUtils.Multiply(ref transformA, wALocal);
                v.WB = MathUtils.Multiply(ref transformB, wBLocal);
                v.W = (Vector2)(v.WB - v.WA);
                V[0] = v;
                Count = 1;
            }
        }

        internal void WriteCache(ref SimplexCache cache)
        {
            cache.Metric = GetMetric();
            cache.Count = (ushort)Count;
            for (var i = 0; i < Count; ++i)
            {
                cache.IndexA[i] = (byte)(V[i].IndexA);
                cache.IndexB[i] = (byte)(V[i].IndexB);
            }
        }

        internal Vector2 GetSearchDirection()
        {
            switch (Count)
            {
                case 1:
                    return -V[0].W;

                case 2:
                    var e12 = V[1].W - V[0].W;
                    return (MathUtils.Cross(e12, -V[0].W) > 0.0f)
                               ? new Vector2(-e12.Y, e12.X) // Origin is left of e12.
                               : new Vector2(e12.Y, -e12.X); // Origin is right of e12.

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal Vector2 GetClosestPoint()
        {
            switch (Count)
            {
                case 1:
                    return V[0].W;

                case 2:
                    return V[0].A * V[0].W + V[1].A * V[1].W;

                case 3:
                    return Vector2.Zero;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void GetWitnessPoints(out WorldVector2 pA, out WorldVector2 pB)
        {
            switch (Count)
            {
                case 1:
                    pA = V[0].WA;
                    pB = V[0].WB;
                    break;

                case 2:
                    pA = V[0].A * V[0].WA + V[1].A * V[1].WA;
                    pB = V[0].A * V[0].WB + V[1].A * V[1].WB;
                    break;

                case 3:
                    pA = V[0].A * V[0].WA + V[1].A * V[1].WA + V[2].A * V[2].WA;
                    pB = pA;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float GetMetric()
        {
            switch (Count)
            {
                case 1:
                    return 0.0f;

                case 2:
                    return (V[0].W - V[1].W).Length();

                case 3:
                    return MathUtils.Cross(V[1].W - V[0].W, V[2].W - V[0].W);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Solve a line segment using barycentric coordinates.
        //
        // p = a1 * w1 + a2 * w2
        // a1 + a2 = 1
        //
        // The vector from the origin to the closest point on the line is
        // perpendicular to the line.
        // e12 = w2 - w1
        // dot(p, e) = 0
        // a1 * dot(w1, e) + a2 * dot(w2, e) = 0
        //
        // 2-by-2 linear system
        // [1      1     ][a1] = [1]
        // [w1.e12 w2.e12][a2] = [0]
        //
        // Define
        // d12_1 =  dot(w2, e12)
        // d12_2 = -dot(w1, e12)
        // d12 = d12_1 + d12_2
        //
        // Solution
        // a1 = d12_1 / d12
        // a2 = d12_2 / d12

        internal void Solve2()
        {
            var w1 = V[0].W;
            var w2 = V[1].W;
            var e12 = w2 - w1;

            // w1 region
            var d12_2 = -Vector2.Dot(w1, e12);
            if (d12_2 <= 0.0f)
            {
                // a2 <= 0, so we clamp it to 0
                SimplexVertex v0 = V[0];
                v0.A = 1.0f;
                V[0] = v0;
                Count = 1;
                return;
            }

            // w2 region
            var d12_1 = Vector2.Dot(w2, e12);
            if (d12_1 <= 0.0f)
            {
                // a1 <= 0, so we clamp it to 0
                SimplexVertex v1 = V[1];
                v1.A = 1.0f;
                V[1] = v1;
                Count = 1;
                V[0] = V[1];
                return;
            }

            // Must be in e12 region.
            var inv_d12 = 1.0f / (d12_1 + d12_2);
            var v0_2 = V[0];
            var v1_2 = V[1];
            v0_2.A = d12_1 * inv_d12;
            v1_2.A = d12_2 * inv_d12;
            V[0] = v0_2;
            V[1] = v1_2;
            Count = 2;
        }

        // Possible regions:
        // - points[2]
        // - edge points[0]-points[2]
        // - edge points[1]-points[2]
        // - inside the triangle
        internal void Solve3()
        {
            var w1 = V[0].W;
            var w2 = V[1].W;
            var w3 = V[2].W;

            // Edge12
            // [1      1     ][a1] = [1]
            // [w1.e12 w2.e12][a2] = [0]
            // a3 = 0
            var e12 = w2 - w1;
            var w1e12 = Vector2.Dot(w1, e12);
            var w2e12 = Vector2.Dot(w2, e12);
            var d12_1 = w2e12;
            var d12_2 = -w1e12;

            // Edge13
            // [1      1     ][a1] = [1]
            // [w1.e13 w3.e13][a3] = [0]
            // a2 = 0
            var e13 = w3 - w1;
            var w1e13 = Vector2.Dot(w1, e13);
            var w3e13 = Vector2.Dot(w3, e13);
            var d13_1 = w3e13;
            var d13_2 = -w1e13;

            // Edge23
            // [1      1     ][a2] = [1]
            // [w2.e23 w3.e23][a3] = [0]
            // a1 = 0
            var e23 = w3 - w2;
            var w2e23 = Vector2.Dot(w2, e23);
            var w3e23 = Vector2.Dot(w3, e23);
            var d23_1 = w3e23;
            var d23_2 = -w2e23;

            // Triangle123
            var n123 = MathUtils.Cross(e12, e13);

            var d123_1 = n123 * MathUtils.Cross(w2, w3);
            var d123_2 = n123 * MathUtils.Cross(w3, w1);
            var d123_3 = n123 * MathUtils.Cross(w1, w2);

            // w1 region
            if (d12_2 <= 0.0f && d13_2 <= 0.0f)
            {
                var v0_1 = V[0];
                v0_1.A = 1.0f;
                V[0] = v0_1;
                Count = 1;
                return;
            }

            // e12
            if (d12_1 > 0.0f && d12_2 > 0.0f && d123_3 <= 0.0f)
            {
                var inv_d12 = 1.0f / (d12_1 + d12_2);
                var v0_2 = V[0];
                var v1_2 = V[1];
                v0_2.A = d12_1 * inv_d12;
                v1_2.A = d12_2 * inv_d12;
                V[0] = v0_2;
                V[1] = v1_2;
                Count = 2;
                return;
            }

            // e13
            if (d13_1 > 0.0f && d13_2 > 0.0f && d123_2 <= 0.0f)
            {
                var inv_d13 = 1.0f / (d13_1 + d13_2);
                var v0_3 = V[0];
                var v2_3 = V[2];
                v0_3.A = d13_1 * inv_d13;
                v2_3.A = d13_2 * inv_d13;
                V[0] = v0_3;
                V[2] = v2_3;
                Count = 2;
                V[1] = V[2];
                return;
            }

            // w2 region
            if (d12_1 <= 0.0f && d23_2 <= 0.0f)
            {
                SimplexVertex v1_4 = V[1];
                v1_4.A = 1.0f;
                V[1] = v1_4;
                Count = 1;
                V[0] = V[1];
                return;
            }

            // w3 region
            if (d13_1 <= 0.0f && d23_1 <= 0.0f)
            {
                SimplexVertex v2_5 = V[2];
                v2_5.A = 1.0f;
                V[2] = v2_5;
                Count = 1;
                V[0] = V[2];
                return;
            }

            // e23
            if (d23_1 > 0.0f && d23_2 > 0.0f && d123_1 <= 0.0f)
            {
                float inv_d23 = 1.0f / (d23_1 + d23_2);
                SimplexVertex v1_6 = V[1];
                SimplexVertex v2_6 = V[2];
                v1_6.A = d23_1 * inv_d23;
                v2_6.A = d23_2 * inv_d23;
                V[1] = v1_6;
                V[2] = v2_6;
                Count = 2;
                V[0] = V[2];
                return;
            }

            // Must be in triangle123
            float inv_d123 = 1.0f / (d123_1 + d123_2 + d123_3);
            SimplexVertex v0_7 = V[0];
            SimplexVertex v1_7 = V[1];
            SimplexVertex v2_7 = V[2];
            v0_7.A = d123_1 * inv_d123;
            v1_7.A = d123_2 * inv_d123;
            v2_7.A = d123_3 * inv_d123;
            V[0] = v0_7;
            V[1] = v1_7;
            V[2] = v2_7;
            Count = 3;
        }
    }

    internal static class Distance
    {
        public static void ComputeDistance(out DistanceOutput output,
                                           out SimplexCache cache,
                                           DistanceInput input)
        {
            cache = new SimplexCache();

            // Initialize the simplex.
            var simplex = new Simplex();
            simplex.ReadCache(ref cache, input.ProxyA, ref input.TransformA, input.ProxyB, ref input.TransformB);

            // Get simplex vertices as an array.
            const int kMaxIters = 20;

            // These store the vertices of the last simplex so that we
            // can check for duplicates and prevent cycling.
            var saveA = new FixedArray3<int>();
            var saveB = new FixedArray3<int>();

            var closestPoint = simplex.GetClosestPoint();
            var distanceSqr1 = closestPoint.LengthSquared();

            // Main iteration loop.
            var iter = 0;
            while (iter < kMaxIters)
            {
                // Copy simplex so we can identify duplicates.
                int saveCount = simplex.Count;
                for (int i = 0; i < saveCount; ++i)
                {
                    saveA[i] = simplex.V[i].IndexA;
                    saveB[i] = simplex.V[i].IndexB;
                }

                switch (simplex.Count)
                {
                    case 1:
                        break;

                    case 2:
                        simplex.Solve2();
                        break;

                    case 3:
                        simplex.Solve3();
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }

                // If we have 3 points, then the origin is in the corresponding triangle.
                if (simplex.Count == 3)
                {
                    break;
                }

                // Compute closest point.
                var p = simplex.GetClosestPoint();
                float distanceSqr2 = p.LengthSquared();

                // Ensure progress
                if (distanceSqr2 >= distanceSqr1)
                {
                    //break;
                }
                distanceSqr1 = distanceSqr2;

                // Get search direction.
                var d = simplex.GetSearchDirection();

                // Ensure the search direction is numerically fit.
                if (d.LengthSquared() < Settings.Epsilon * Settings.Epsilon)
                {
                    // The origin is probably contained by a line segment
                    // or triangle. Thus the shapes are overlapped.

                    // We can't return zero here even though there may be overlap.
                    // In case the simplex is a point, segment, or triangle it is difficult
                    // to determine if the origin is contained in the CSO or very close to it.
                    break;
                }

                // Compute a tentative new simplex vertex using support points.
                var vertex = simplex.V[simplex.Count];
                vertex.IndexA = input.ProxyA.GetSupport(MathUtils.MultiplyT(ref input.TransformA.R, -d));
                vertex.WA = MathUtils.Multiply(ref input.TransformA, input.ProxyA.Vertices[vertex.IndexA]);

                vertex.IndexB = input.ProxyB.GetSupport(MathUtils.MultiplyT(ref input.TransformB.R, d));
                vertex.WB = MathUtils.Multiply(ref input.TransformB, input.ProxyB.Vertices[vertex.IndexB]);
                vertex.W = (Vector2)(vertex.WB - vertex.WA);
                simplex.V[simplex.Count] = vertex;

                // Iteration count is equated to the number of support point calls.
                ++iter;

                // Check for duplicate support points. This is the main termination criteria.
                var duplicate = false;
                for (var i = 0; i < saveCount; ++i)
                {
                    if (vertex.IndexA == saveA[i] && vertex.IndexB == saveB[i])
                    {
                        duplicate = true;
                        break;
                    }
                }

                // If we found a duplicate support point we must exit to avoid cycling.
                if (duplicate)
                {
                    break;
                }

                // New vertex is ok and needed.
                ++simplex.Count;
            }

            // Prepare output.
            simplex.GetWitnessPoints(out output.PointA, out output.PointB);
            output.Distance = ((Vector2)(output.PointA - output.PointB)).Length();

            // Cache the simplex.
            simplex.WriteCache(ref cache);

            // Apply radii if requested.
            if (input.UseRadii)
            {
                var rA = input.ProxyA.Radius;
                var rB = input.ProxyB.Radius;

                if (output.Distance > rA + rB && output.Distance > Settings.Epsilon)
                {
                    // Shapes are still no overlapped.
                    // Move the witness points to the outer surface.
                    output.Distance -= rA + rB;
                    var normal = (Vector2)(output.PointB - output.PointA);
                    normal.Normalize();
                    output.PointA += rA * normal;
                    output.PointB -= rB * normal;
                }
                else
                {
                    // Shapes are overlapped when radii are considered.
                    // Move the witness points to the middle.
                    var p = output.PointA + 0.5f * (Vector2)(output.PointB - output.PointA);
                    output.PointA = p;
                    output.PointB = p;
                    output.Distance = 0.0f;
                }
            }
        }
    }
}