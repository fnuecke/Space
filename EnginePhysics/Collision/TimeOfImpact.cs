using System;
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
            var cache = new SimplexCache {Count = 0};

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
                var distance = Distance(ref cache, proxyA, ref xfA, proxyB, ref xfB);

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
                SeparationFunction.Initialize(out fcn, ref cache, proxyA, proxyB, ref sweepA, ref sweepB, t1);

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

        private struct SeparationFunction
        {
            private enum Type
            {
                Points,

                FaceA,

                FaceB
            };

            private Vector2 _axis;

            private LocalPoint _localPoint;

            private DistanceProxy _proxyA, _proxyB;

            private Sweep _sweepA, _sweepB;

            private Type _type;

            public static void Initialize(out SeparationFunction f, ref SimplexCache cache,
                                          DistanceProxy proxyA, DistanceProxy proxyB,
                                          ref Sweep sweepA, ref Sweep sweepB, float t1)
            {
                System.Diagnostics.Debug.Assert(0 < cache.Count && cache.Count < 3);

                f._proxyA = proxyA;
                f._proxyB = proxyB;

                f._sweepA = sweepA;
                f._sweepB = sweepB;

                WorldTransform xfA, xfB;
                f._sweepA.GetTransform(out xfA, t1);
                f._sweepB.GetTransform(out xfB, t1);

                if (cache.Count == 1)
                {
                    f._type = Type.Points;
                    var localPointA = f._proxyA.GetVertex(cache.IndexA.Item1);
                    var localPointB = f._proxyB.GetVertex(cache.IndexB.Item1);
                    var pointA = xfA.ToGlobal(localPointA);
                    var pointB = xfB.ToGlobal(localPointB);
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    f._axis = (Vector2)(pointB - pointA);
// ReSharper restore RedundantCast
                    f._axis.Normalize();
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
                    
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    var s = Vector2.Dot((Vector2)(pointA - pointB), normal);
// ReSharper restore RedundantCast
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
                    
// ReSharper disable RedundantCast Necessary for FarPhysics.
                    var s = Vector2.Dot((Vector2)(pointB - pointA), normal);
// ReSharper restore RedundantCast
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
                        
// ReSharper disable RedundantCast Necessary for FarPhysics.
                        return Vector2.Dot((Vector2)(pointB - pointA), _axis);
// ReSharper restore RedundantCast
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
                        
// ReSharper disable RedundantCast Necessary for FarPhysics.
                        return Vector2.Dot((Vector2)(pointB - pointA), normal);
// ReSharper restore RedundantCast
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
                        
// ReSharper disable RedundantCast Necessary for FarPhysics.
                        return Vector2.Dot((Vector2)(pointA - pointB), normal);
// ReSharper restore RedundantCast
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
// ReSharper disable RedundantCast Necessary for FarPhysics.
                        return Vector2.Dot((Vector2)(pointB - pointA), _axis);
// ReSharper restore RedundantCast
                    }

                    case Type.FaceA:
                    {
                        var normal = xfA.Rotation * _axis;
                        var pointA = xfA.ToGlobal(_localPoint);

                        var localPointB = _proxyB.GetVertex(indexB);
                        var pointB = xfB.ToGlobal(localPointB);
                        
// ReSharper disable RedundantCast Necessary for FarPhysics.
                        return Vector2.Dot((Vector2)(pointB - pointA), normal);
// ReSharper restore RedundantCast
                    }

                    case Type.FaceB:
                    {
                        var normal = xfB.Rotation * _axis;
                        var pointB = xfB.ToGlobal(_localPoint);

                        var localPointA = _proxyA.GetVertex(indexA);
                        var pointA = xfA.ToGlobal(localPointA);
                        
// ReSharper disable RedundantCast Necessary for FarPhysics.
                        return Vector2.Dot((Vector2)(pointA - pointB), normal);
// ReSharper restore RedundantCast
                    }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
