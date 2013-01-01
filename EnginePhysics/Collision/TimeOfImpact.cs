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
        // CCD via the local separating axis method. This seeks progression
        // by computing the largest time at which separation is maintained.
        public static bool TimeOfImpact(DistanceProxy proxyA, DistanceProxy proxyB,
                                        Sweep sweepA, Sweep sweepB,
                                        float tMax, out float t)
        {
            // Large rotations can make the root finder fail, so we normalize the
            // sweep angles.
            sweepA.Normalize();
            sweepB.Normalize();

            var totalRadius = proxyA.Radius + proxyB.Radius;
            var target = System.Math.Max(Settings.LinearSlop, totalRadius - 3.0f * Settings.LinearSlop);

            const float tolerance = 0.25f * Settings.LinearSlop;
            System.Diagnostics.Debug.Assert(target > tolerance);

            // Prepare input for distance query.
            var cache = new SimplexCache{Count = 0};

            // The outer loop progressively attempts to compute new separating axes.
            // This loop terminates when an axis is repeated (no progress is made).
            var t1 = 0.0f;
            for (var iter = 0; iter < 20; ++iter)
            {
                WorldTransform xfA, xfB;
                sweepA.GetTransform(out xfA, t1);
                sweepB.GetTransform(out xfB, t1);

                // Get the distance between shapes. We can also use the results
                // to get a separating axis.
                var distance = Distance(ref cache, proxyA, proxyB, xfA, xfB);

                // If the shapes are overlapped, we give up on continuous collision.
                if (distance <= 0.0f)
                {
                    // Failure!
                    t = 0.0f;
                    return false;
                }

                if (distance < target + tolerance)
                {
                    // Victory!
                    t = t1;
                    return true;
                }

                // Initialize the separating axis.
                SeparationFunction fcn;
                SeparationFunction.Initialize(cache, proxyA, sweepA, proxyB, sweepB, t1, out fcn);

                // Compute the TOI on the separating axis. We do this by successively
                // resolving the deepest point. This loop is bounded by the number of vertices.
                var t2 = tMax;
                for (var pushBackIter = 0; pushBackIter < Settings.MaxPolygonVertices; ++pushBackIter)
                {
                    // Find the deepest point at t2. Store the witness point indices.
                    int indexA, indexB;
                    var s2 = fcn.FindMinSeparation(out indexA, out indexB, t2);

                    // Is the final configuration separated?
                    if (s2 > target + tolerance)
                    {
                        // Victory!
                        t = tMax;
                        return false;
                    }

                    // Has the separation reached tolerance?
                    if (s2 > target - tolerance)
                    {
                        // Advance the sweeps
                        t1 = t2;
                        break;
                    }

                    // Compute the initial separation of the witness points.
                    var s1 = fcn.Evaluate(indexA, indexB, t1);

                    // Check for initial overlap. This might happen if the root finder
                    // runs out of iterations.
                    if (s1 < target - tolerance)
                    {
                        t = t1;
                        return false;
                    }

                    // Check for touching
                    if (s1 <= target + tolerance)
                    {
                        // Victory! t1 should hold the TOI (could be 0.0).
                        t = t1;
                        return true;
                    }

                    // Compute 1D root of: f(x) - target = 0
                    float a1 = t1, a2 = t2;
                    for (var rootIterCount = 0; rootIterCount < 50; ++rootIterCount)
                    {
                        // Use a mix of the secant rule and bisection.
                        float u;
                        if ((rootIterCount & 1) != 0)
                        {
                            // Secant rule to improve convergence.
                            u = a1 + (target - s1) * (a2 - a1) / (s2 - s1);
                        }
                        else
                        {
                            // Bisection to guarantee progress.
                            u = 0.5f * (a1 + a2);
                        }

                        var s = fcn.Evaluate(indexA, indexB, u);

                        if (System.Math.Abs(s - target) < tolerance)
                        {
                            // t2 holds a tentative value for t1
                            t2 = u;
                            break;
                        }

                        // Ensure we continue to bracket the root.
                        if (s > target)
                        {
                            a1 = u;
                            s1 = s;
                        }
                        else
                        {
                            a2 = u;
                            s2 = s;
                        }
                    }
                }
            }

            // Root finder got stuck. Semi-victory.
            t = t1;
            return false;
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
                        _vertices = _buffer;
                        _buffer[0] = circle.Center;
                        _count = 1;
                        Radius = circle.Radius;
                    }
                        break;

                    case Fixture.FixtureType.Polygon:
                    {
                        var polygon = (PolygonFixture)fixture;
                        _vertices = polygon.Vertices;
                        _count = polygon.Count;
                        Radius = polygon.Radius;
                    }
                        break;

                    case Fixture.FixtureType.Edge:
                    {
                        var edge = (EdgeFixture)fixture;
                        _vertices = _buffer;
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

            /// Get the supporting vertex in the given direction.
            public LocalPoint GetSupportVertex(Vector2 d)
            {
                return _vertices[GetSupport(d)];
            }

            /// Get the vertex count.
            public int GetVertexCount()
            {
                return _count;
            }

            /// Get a vertex by index. Used by b2Distance.
            public LocalPoint GetVertex(int index)
            {
                System.Diagnostics.Debug.Assert(0 <= index && index < _count);
                return _vertices[index];
            }

            /// <summary>
            /// Local array used for circle and edge shape. For polygon shapes
            /// we can just set it to the fixture's array.
            /// </summary>
            private readonly LocalPoint[] _buffer = new LocalPoint[2];

            /// <summary>
            /// The list of vertices describing this shape.
            /// </summary>
            private LocalPoint[] _vertices;

            /// <summary>
            /// The number of actual vertices (may differ from _vertices.Length).
            /// </summary>
            private int _count;

            /// <summary>
            /// Buffer radius around the edges of the shape (?)
            /// </summary>
            internal float Radius;
        }

        /// <summary>
        /// Used to warm start b2Distance.
        /// </summary>
        private struct SimplexCache
        {
            /// <summary>
            /// Length or area.
            /// </summary>
            public float Metric;

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
        }

        private struct SeparationFunction
        {
            private enum Type
            {
                Points,

                FaceA,

                FaceB
            };

            private DistanceProxy _proxyA;

            private DistanceProxy _proxyB;

            private Sweep _sweepA, _sweepB;

            private Type _type;

            private LocalPoint _localPoint;

            private Vector2 _axis;

            public static void Initialize(SimplexCache cache,
                                          DistanceProxy proxyA, Sweep sweepA,
                                          DistanceProxy proxyB, Sweep sweepB,
                                          float t1, out SeparationFunction f)
            {
                System.Diagnostics.Debug.Assert(0 < cache.Count && cache.Count < 3);

                f._proxyA = proxyA;
                f._proxyB = proxyB;
                var count = cache.Count;

                f._sweepA = sweepA;
                f._sweepB = sweepB;

                WorldTransform xfA, xfB;
                f._sweepA.GetTransform(out xfA, t1);
                f._sweepB.GetTransform(out xfB, t1);

                if (count == 1)
                {
                    f._type = Type.Points;
                    var localPointA = f._proxyA.GetVertex(cache.IndexA.Item1);
                    var localPointB = f._proxyB.GetVertex(cache.IndexB.Item1);
                    var pointA = xfA.ToGlobal(localPointA);
                    var pointB = xfB.ToGlobal(localPointB);
                    f._axis = (Vector2)(pointB - pointA);
                    var s = f._axis.Length();
                    f._axis /= s;
                    f._localPoint = LocalPoint.Zero;
                }
                else if (cache.IndexA.Item1 == cache.IndexA.Item2)
                {
                    // Two points on B and one on A.
                    f._type = Type.FaceB;
                    var localPointB1 = proxyB.GetVertex(cache.IndexB.Item1);
                    var localPointB2 = proxyB.GetVertex(cache.IndexB.Item2);

                    f._axis = Vector2Util.Cross(localPointB2 - localPointB1, 1.0f);
                    f._axis.Normalize();
                    var normal = xfB.Rotation * f._axis;

                    f._localPoint = 0.5f * (localPointB1 + localPointB2);
                    var pointB = xfB.ToGlobal(f._localPoint);

                    var localPointA = proxyA.GetVertex(cache.IndexA[0]);
                    var pointA = xfA.ToGlobal(localPointA);

                    var s = Vector2.Dot((Vector2)(pointA - pointB), normal);
                    if (s < 0.0f)
                    {
                        f._axis = -f._axis;
                    }
                }
                else
                {
                    // Two points on A and one or two points on B.
                    f._type = Type.FaceA;
                    var localPointA1 = f._proxyA.GetVertex(cache.IndexA.Item1);
                    var localPointA2 = f._proxyA.GetVertex(cache.IndexA.Item2);

                    f._axis = Vector2Util.Cross(localPointA2 - localPointA1, 1.0f);
                    f._axis.Normalize();
                    var normal = xfA.Rotation * f._axis;

                    f._localPoint = 0.5f * (localPointA1 + localPointA2);
                    var pointA = xfA.ToGlobal(f._localPoint);

                    var localPointB = f._proxyB.GetVertex(cache.IndexB[0]);
                    var pointB = xfB.ToGlobal(localPointB);

                    var s = Vector2.Dot((Vector2)(pointB - pointA), normal);
                    if (s < 0.0f)
                    {
                        f._axis = -f._axis;
                    }
                }
            }

            public float FindMinSeparation(out int indexA, out int indexB, float t)
            {
                WorldTransform xfA, xfB;
                _sweepA.GetTransform(out xfA, t);
                _sweepB.GetTransform(out xfB, t);

                switch (_type)
                {
                    case Type.Points:
                    {
                        var axisA = -xfA.Rotation * _axis;
                        var axisB = -xfB.Rotation * -_axis;

                        indexA = _proxyA.GetSupport(axisA);
                        indexB = _proxyB.GetSupport(axisB);

                        var localPointA = _proxyA.GetVertex(indexA);
                        var localPointB = _proxyB.GetVertex(indexB);

                        var pointA = xfA.ToGlobal(localPointA);
                        var pointB = xfB.ToGlobal(localPointB);

                        return Vector2.Dot((Vector2)(pointB - pointA), _axis);
                    }

                    case Type.FaceA:
                    {
                        var normal = xfA.Rotation * _axis;
                        var pointA = xfA.ToGlobal(_localPoint);

                        var axisB = -xfB.Rotation * -normal;

                        indexA = -1;
                        indexB = _proxyB.GetSupport(axisB);

                        var localPointB = _proxyB.GetVertex(indexB);
                        var pointB = xfB.ToGlobal(localPointB);

                        return Vector2.Dot((Vector2)(pointB - pointA), normal);
                    }

                    case Type.FaceB:
                    {
                        var normal = xfB.Rotation * _axis;
                        var pointB = xfB.ToGlobal(_localPoint);

                        var axisA = -xfA.Rotation * -normal;

                        indexB = -1;
                        indexA = _proxyA.GetSupport(axisA);

                        var localPointA = _proxyA.GetVertex(indexA);
                        var pointA = xfA.ToGlobal(localPointA);

                        return Vector2.Dot((Vector2)(pointA - pointB), normal);
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public float Evaluate(int indexA, int indexB, float t)
            {
                WorldTransform xfA, xfB;
                _sweepA.GetTransform(out xfA, t);
                _sweepB.GetTransform(out xfB, t);

                switch (_type)
                {
                    case Type.Points:
                    {
                        var localPointA = _proxyA.GetVertex(indexA);
                        var localPointB = _proxyB.GetVertex(indexB);

                        var pointA = xfA.ToGlobal(localPointA);
                        var pointB = xfB.ToGlobal(localPointB);
                        return Vector2.Dot((Vector2)(pointB - pointA), _axis);
                    }

                    case Type.FaceA:
                    {
                        var normal = xfA.Rotation * _axis;
                        var pointA = xfA.ToGlobal(_localPoint);

                        var localPointB = _proxyB.GetVertex(indexB);
                        var pointB = xfB.ToGlobal(localPointB);

                        return Vector2.Dot((Vector2)(pointB - pointA), normal);
                    }

                    case Type.FaceB:
                    {
                        var normal = xfB.Rotation * _axis;
                        var pointB = xfB.ToGlobal(_localPoint);

                        var localPointA = _proxyA.GetVertex(indexA);
                        var pointA = xfA.ToGlobal(localPointA);

                        return Vector2.Dot((Vector2)(pointA - pointB), normal);
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static float Distance(ref SimplexCache cache,
                                      DistanceProxy proxyA, DistanceProxy proxyB,
                                      WorldTransform transformA, WorldTransform transformB,
                                      bool useRadii = false)
        {
            // Initialize the simplex.
            Simplex simplex;
            Simplex.ReadCache(cache, proxyA, transformA, proxyB, transformB, out simplex);
            
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
                v.IndexA = proxyA.GetSupport(-transformA.Rotation * -d);
                v.IndexB = proxyB.GetSupport(-transformB.Rotation * d);
                v.VertexA = transformA.ToGlobal(proxyA.GetVertex(v.IndexA));
                v.VertexB = transformB.ToGlobal(proxyB.GetVertex(v.IndexB));
                v.VertexDelta = (LocalPoint)(v.VertexB - v.VertexA);
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

        private struct Simplex
        {
            internal FixedArray3<SimplexVertex> Vertices;

            internal int Count;

            public static void ReadCache(SimplexCache cache,
                                         DistanceProxy proxyA, WorldTransform transformA,
                                         DistanceProxy proxyB, WorldTransform transformB,
                                         out Simplex simplex)
            {
                System.Diagnostics.Debug.Assert(cache.Count <= 3);

                // Copy data from cache.
                simplex = new Simplex {Count = cache.Count};
                for (var i = 0; i < simplex.Count; ++i)
                {
                    SimplexVertex v;
                    v.IndexA = cache.IndexA[i];
                    v.IndexB = cache.IndexB[i];
                    v.VertexA = transformA.ToGlobal(proxyA.GetVertex(v.IndexA));
                    v.VertexB = transformB.ToGlobal(proxyB.GetVertex(v.IndexB));
                    v.VertexDelta = (LocalPoint)(v.VertexB - v.VertexA);
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
                    v.VertexA = transformA.ToGlobal(proxyA.GetVertex(0));
                    v.VertexB = transformB.ToGlobal(proxyB.GetVertex(0));
                    v.VertexDelta = (LocalPoint)(v.VertexB - v.VertexA);
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
                        return ((Vector2)(Vertices.Item1.VertexB - Vertices.Item1.VertexA)).Length();

                    case 2:
                        return ((Vector2)((Vertices.Item1.Alpha * Vertices.Item1.VertexB +
                                           Vertices.Item2.Alpha * Vertices.Item2.VertexB) -
                                          (Vertices.Item1.Alpha * Vertices.Item1.VertexA +
                                           Vertices.Item2.Alpha * Vertices.Item2.VertexA))).Length();

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
            /// Support point in proxyA in world space.
            /// </summary>
            public WorldPoint VertexA;

            /// <summary>
            /// Support point in proxyB in world space.
            /// </summary>
            public WorldPoint VertexB;

            /// <summary>
            /// wB - wA
            /// </summary>
            public LocalPoint VertexDelta;

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
        }
    }
}
