using System;
using Engine.Collections;
using Engine.Physics.Components;
using Engine.Physics.Math;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.Physics.Collision
{
    internal static partial class Algorithms
    {
        private static float Distance(ref SimplexCache cache, DistanceProxy proxyA, ref WorldTransform xfA, DistanceProxy proxyB, ref WorldTransform xfB, bool useRadii = false)
        {
            // Initialize the simplex.
            Simplex simplex;
            Simplex.ReadCache(out simplex, ref cache, ref xfA, proxyA, ref xfB, proxyB);
            
            // These store the vertices of the last simplex so that we
            // can check for duplicates and prevent cycling.
            var saveA = new FixedArray3<int>();
            var saveB = new FixedArray3<int>();

            // Main iteration loop.
            for (var iter = 0; iter < 20; ++iter)
            {
                // Copy simplex so we can identify duplicates.
                var saveCount = simplex.Count;
                for (var i = 0; i < saveCount; ++i)
                {
                    saveA[i] = simplex.Vertices[i].IndexA;
                    saveB[i] = simplex.Vertices[i].IndexB;
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
                        throw new ArgumentOutOfRangeException();
                }

                // If we have 3 points, then the origin is in the corresponding triangle.
                if (simplex.Count == 3)
                {
                    break;
                }

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
                SimplexVertex v;
                v.IndexA = proxyA.GetSupport(-xfA.Rotation * -d);
                v.IndexB = proxyB.GetSupport(-xfB.Rotation * d);
                v.VertexA = xfA.ToGlobal(proxyA.GetVertex(v.IndexA));
                v.VertexB = xfB.ToGlobal(proxyB.GetVertex(v.IndexB));
// ReSharper disable RedundantCast Necessary for FarPhysics.
                v.VertexDelta = (LocalPoint)(v.VertexB - v.VertexA);
// ReSharper restore RedundantCast
                v.Alpha = 0;
                simplex.Vertices[simplex.Count] = v;

                // Check for duplicate support points. This is the main termination criteria.
                var duplicate = false;
                for (var i = 0; i < saveCount; ++i)
                {
                    if (v.IndexA == saveA[i] && v.IndexB == saveB[i])
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

            // Cache the simplex.
            simplex.WriteCache(ref cache);

            // Prepare output.
            var distance = simplex.GetWitnessPointDistance();

            // Apply radii if requested.
            if (useRadii)
            {
                var rA = proxyA.Radius;
                var rB = proxyB.Radius;

                if (distance > rA + rB && distance > Settings.Epsilon)
                {
                    // Shapes are still not overlapped.
                    return distance - (rA + rB);
                }
                else
                {
                    // Shapes are overlapped when radii are considered.
                    return 0.0f;
                }
            }

            return distance;
        }

        /// A distance proxy is used by the GJK algorithm.
        /// It encapsulates any shape.
        public sealed class DistanceProxy
        {
            /// <summary>
            /// Initialize the proxy using the given shape.
            /// </summary>
            /// <param name="fixture">The fixture describing the shape.</param>
            public void Set(Fixture fixture)
            {
                switch (fixture.Type)
                {
                    case Fixture.FixtureType.Circle:
                    {
                        var circle = (CircleFixture)fixture;
                        _vertices[0] = circle.Center;
                        _count = 1;
                        Radius = circle.Radius;
                    }
                        break;

                    case Fixture.FixtureType.Polygon:
                    {
                        var polygon = (PolygonFixture)fixture;
                        for (var i = 0; i < polygon.Count; i++)
                        {
                            _vertices[i] = polygon.Vertices[i];
                        }
                        _count = polygon.Count;
                        Radius = polygon.Radius;
                    }
                        break;

                    case Fixture.FixtureType.Edge:
                    {
                        var edge = (EdgeFixture)fixture;
                        _vertices[0] = edge.Vertex1;
                        _vertices[1] = edge.Vertex2;
                        _count = 2;
                        Radius = edge.Radius;
                    }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            /// Get the supporting vertex index in the given direction.
            public int GetSupport(Vector2 d)
            {
                var bestIndex = 0;
                var bestValue = Vector2.Dot(_vertices[0], d);
                for (var i = 1; i < _count; ++i)
                {
                    var value = Vector2.Dot(_vertices[i], d);
                    if (value > bestValue)
                    {
                        bestIndex = i;
                        bestValue = value;
                    }
                }

                return bestIndex;
            }

            /// Get a vertex by index. Used by b2Distance.
            /// TODO inline
            public LocalPoint GetVertex(int index)
            {
                System.Diagnostics.Debug.Assert(0 <= index && index < _count);
                return _vertices[index];
            }

            /// <summary>
            /// Buffer radius around the edges of the shape (?)
            /// </summary>
            internal float Radius;

            /// <summary>
            /// The list of vertices describing this shape.
            /// </summary>
            private readonly LocalPoint[] _vertices = new Vector2[Settings.MaxPolygonVertices];

            /// <summary>
            /// The number of actual vertices.
            /// </summary>
            private int _count;
        }

        /// <summary>
        /// Used to warm start <see cref="Algorithms.Distance"/>.
        /// </summary>
        private struct SimplexCache
        {
            /// <summary>
            /// Number of vertices.
            /// </summary>
            public ushort Count;

            /// <summary>
            /// Vertices on shape A.
            /// </summary>
            public FixedArray3<byte> IndexA;

            /// <summary>
            /// Vertices on shape B.
            /// </summary>
            public FixedArray3<byte> IndexB;

            /// <summary>
            /// Length or area.
            /// </summary>
            public float Metric;
        }

        private struct Simplex
        {
            /// <summary>Number of vertices in this simplex.</summary>
            internal int Count;

            /// <summary>Vertices in this simplex.</summary>
            internal FixedArray3<SimplexVertex> Vertices;

            public static void ReadCache(out Simplex simplex, ref SimplexCache cache,
                                         ref WorldTransform xfA, DistanceProxy proxyA,
                                         ref WorldTransform xfB, DistanceProxy proxyB)
            {
                System.Diagnostics.Debug.Assert(cache.Count <= 3);

                // Copy data from cache.
                simplex = new Simplex {Count = cache.Count};
                for (var i = 0; i < simplex.Count; ++i)
                {
                    SimplexVertex v;
                    v.IndexA = cache.IndexA[i];
                    v.IndexB = cache.IndexB[i];
                    v.VertexA = xfA.ToGlobal(proxyA.GetVertex(v.IndexA));
                    v.VertexB = xfB.ToGlobal(proxyB.GetVertex(v.IndexB));
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    v.VertexDelta = (LocalPoint)(v.VertexB - v.VertexA);
// ReSharper restore RedundantCast
                    v.Alpha = 0.0f;
                    simplex.Vertices[i] = v;
                }

                // Compute the new simplex metric, if it is substantially different than
                // old metric then flush the simplex.
                if (simplex.Count > 1)
                {
                    var metric1 = cache.Metric;
                    var metric2 = simplex.GetMetric();
                    if (metric2 < 0.5f * metric1 || 2.0f * metric1 < metric2 || metric2 < float.Epsilon)
                    {
                        // Reset the simplex, triggers computation below.
                        simplex.Count = 0;
                    }
                }

                // If the cache is empty or invalid ...
                if (simplex.Count == 0)
                {
                    SimplexVertex v;
                    v.IndexA = 0;
                    v.IndexB = 0;
                    v.VertexA = xfA.ToGlobal(proxyA.GetVertex(0));
                    v.VertexB = xfB.ToGlobal(proxyB.GetVertex(0));
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    v.VertexDelta = (LocalPoint)(v.VertexB - v.VertexA);
// ReSharper restore RedundantCast
                    v.Alpha = 1.0f;
                    simplex.Vertices.Item1 = v;
                    simplex.Count = 1;
                }
            }

            public void WriteCache(ref SimplexCache cache)
            {
                cache.Metric = GetMetric();
                cache.Count = (ushort)Count;
                for (var i = 0; i < Count; ++i)
                {
                    cache.IndexA[i] = (byte)Vertices[i].IndexA;
                    cache.IndexB[i] = (byte)Vertices[i].IndexB;
                }
            }

            public Vector2 GetSearchDirection()
            {
                switch (Count)
                {
                    case 1:
                        return -Vertices.Item1.VertexDelta;

                    case 2:
                    {
                        var e12 = Vertices.Item2.VertexDelta - Vertices.Item1.VertexDelta;
                        var sgn = Vector2Util.Cross(e12, -Vertices.Item1.VertexDelta);
                        if (sgn > 0.0f)
                        {
                            // Origin is left of e12.
                            return Vector2Util.Cross(1.0f, e12);
                        }
                        else
                        {
                            // Origin is right of e12.
                            return Vector2Util.Cross(e12, 1.0f);
                        }
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public float GetWitnessPointDistance()
            {
                switch (Count)
                {
                    case 1:
                        return WorldPoint.Distance(Vertices.Item1.VertexA, Vertices.Item1.VertexB);

                    case 2:
                        return WorldPoint.Distance((Vertices.Item1.Alpha * Vertices.Item1.VertexA +
                                                    Vertices.Item2.Alpha * Vertices.Item2.VertexA),
                                                   (Vertices.Item1.Alpha * Vertices.Item1.VertexB +
                                                    Vertices.Item2.Alpha * Vertices.Item2.VertexB));

                    case 3:
                        return 0;

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
                        return LocalPoint.Distance(Vertices.Item1.VertexDelta, Vertices.Item2.VertexDelta);

                    case 3:
                        return Vector2Util.Cross(Vertices.Item2.VertexDelta - Vertices.Item1.VertexDelta,
                                                 Vertices.Item3.VertexDelta - Vertices.Item1.VertexDelta);

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
            public void Solve2()
            {
                var w1 = Vertices.Item1.VertexDelta;
                var w2 = Vertices.Item2.VertexDelta;
                var e12 = w2 - w1;

                // w1 region
                var d12_2 = -Vector2.Dot(w1, e12);
                if (d12_2 <= 0.0f)
                {
                    // a2 <= 0, so we clamp it to 0
                    Vertices.Item1.Alpha = 1.0f;
                    Count = 1;
                    return;
                }

                // w2 region
                var d12_1 = Vector2.Dot(w2, e12);
                if (d12_1 <= 0.0f)
                {
                    // a1 <= 0, so we clamp it to 0
                    Vertices.Item2.Alpha = 1.0f;
                    Count = 1;
                    Vertices.Item1 = Vertices.Item2;
                    return;
                }

                // Must be in e12 region.
                var inv_d12 = 1.0f / (d12_1 + d12_2);
                Vertices.Item1.Alpha = d12_1 * inv_d12;
                Vertices.Item2.Alpha = d12_2 * inv_d12;
                Count = 2;
            }

            // Possible regions:
            // - points[2]
            // - edge points[0]-points[2]
            // - edge points[1]-points[2]
            // - inside the triangle
            public void Solve3()
            {
                var w1 = Vertices.Item1.VertexDelta;
                var w2 = Vertices.Item2.VertexDelta;
                var w3 = Vertices.Item3.VertexDelta;

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
                var n123 = Vector2Util.Cross(e12, e13);

                var d123_1 = n123 * Vector2Util.Cross(w2, w3);
                var d123_2 = n123 * Vector2Util.Cross(w3, w1);
                var d123_3 = n123 * Vector2Util.Cross(w1, w2);

                // w1 region
                if (d12_2 <= 0.0f && d13_2 <= 0.0f)
                {
                    Vertices.Item1.Alpha = 1.0f;
                    Count = 1;
                    return;
                }

                // e12
                if (d12_1 > 0.0f && d12_2 > 0.0f && d123_3 <= 0.0f)
                {
                    var inv_d12 = 1.0f / (d12_1 + d12_2);
                    Vertices.Item1.Alpha = d12_1 * inv_d12;
                    Vertices.Item2.Alpha = d12_2 * inv_d12;
                    Count = 2;
                    return;
                }

                // e13
                if (d13_1 > 0.0f && d13_2 > 0.0f && d123_2 <= 0.0f)
                {
                    var inv_d13 = 1.0f / (d13_1 + d13_2);
                    Vertices.Item1.Alpha = d13_1 * inv_d13;
                    Vertices.Item3.Alpha = d13_2 * inv_d13;
                    Count = 2;
                    Vertices.Item2 = Vertices.Item3;
                    return;
                }

                // w2 region
                if (d12_1 <= 0.0f && d23_2 <= 0.0f)
                {
                    Vertices.Item2.Alpha = 1.0f;
                    Count = 1;
                    Vertices.Item1 = Vertices.Item2;
                    return;
                }

                // w3 region
                if (d13_1 <= 0.0f && d23_1 <= 0.0f)
                {
                    Vertices.Item3.Alpha = 1.0f;
                    Count = 1;
                    Vertices.Item1 = Vertices.Item3;
                    return;
                }

                // e23
                if (d23_1 > 0.0f && d23_2 > 0.0f && d123_1 <= 0.0f)
                {
                    var inv_d23 = 1.0f / (d23_1 + d23_2);
                    Vertices.Item2.Alpha = d23_1 * inv_d23;
                    Vertices.Item3.Alpha = d23_2 * inv_d23;
                    Count = 2;
                    Vertices.Item1 = Vertices.Item3;
                    return;
                }

                // Must be in triangle123
                var inv_d123 = 1.0f / (d123_1 + d123_2 + d123_3);
                Vertices.Item1.Alpha = d123_1 * inv_d123;
                Vertices.Item2.Alpha = d123_2 * inv_d123;
                Vertices.Item3.Alpha = d123_3 * inv_d123;
                Count = 3;
            }
        }

        private struct SimplexVertex
        {
            /// <summary>
            /// Barycentric coordinate for closest point.
            /// </summary>
            public float Alpha;

            /// <summary>
            /// VertexA index.
            /// </summary>
            public int IndexA;

            /// <summary>
            /// VertexB index.
            /// </summary>
            public int IndexB;

            /// <summary>
            /// wB - wA
            /// </summary>
            public LocalPoint VertexDelta;

            /// <summary>
            /// Support point in proxyA in world space.
            /// </summary>
            public WorldPoint VertexA;

            /// <summary>
            /// Support point in proxyB in world space.
            /// </summary>
            public WorldPoint VertexB;
        }
    }
}
