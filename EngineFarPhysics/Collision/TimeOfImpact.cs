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
using FarseerPhysics.Common;
using Microsoft.Xna.Framework;
using WorldVector2 = Engine.FarMath.FarPosition;

namespace FarseerPhysics.Collision
{
    /// <summary>
    /// Input parameters for CalculateTimeOfImpact
    /// </summary>
    internal sealed class TOIInput
    {
        public DistanceProxy ProxyA = new DistanceProxy();
        public DistanceProxy ProxyB = new DistanceProxy();
        public Sweep SweepA;
        public Sweep SweepB;
        public float TMax; // defines sweep interval [0, tMax]
    }

    internal enum TOIOutputState
    {
        Unknown,
        Failed,
        Overlapped,
        Touching,
        Seperated,
    }

    public struct TOIOutput
    {
        internal TOIOutputState State;
        public float T;
    }

    internal enum SeparationFunctionType
    {
        Points,
        FaceA,
        FaceB
    }

    internal static class SeparationFunction
    {
        private static Vector2 _axis;
        private static Vector2 _localPoint;
        private static DistanceProxy _proxyA = new DistanceProxy();
        private static DistanceProxy _proxyB = new DistanceProxy();
        private static Sweep _sweepA, _sweepB;
        private static SeparationFunctionType _type;

        public static void Set(ref SimplexCache cache,
                               DistanceProxy proxyA, ref Sweep sweepA,
                               DistanceProxy proxyB, ref Sweep sweepB,
                               float t1)
        {
            _localPoint = Vector2.Zero;
            _proxyA = proxyA;
            _proxyB = proxyB;
            int count = cache.Count;

            Debug.Assert(0 < count && count < 3);

            _sweepA = sweepA;
            _sweepB = sweepB;

            Transform xfA, xfB;
            _sweepA.GetTransform(out xfA, t1);
            _sweepB.GetTransform(out xfB, t1);

            if (count == 1)
            {
                _type = SeparationFunctionType.Points;
                var localPointA = _proxyA.Vertices[cache.IndexA[0]];
                var localPointB = _proxyB.Vertices[cache.IndexB[0]];
                var pointA = MathUtils.Multiply(ref xfA, localPointA);
                var pointB = MathUtils.Multiply(ref xfB, localPointB);
                _axis = (Vector2)(pointB - pointA);
                _axis.Normalize();
            }
            else if (cache.IndexA[0] == cache.IndexA[1])
            {
                // Two points on B and one on A.
                _type = SeparationFunctionType.FaceB;
                var localPointB1 = proxyB.Vertices[cache.IndexB[0]];
                var localPointB2 = proxyB.Vertices[cache.IndexB[1]];

                var a = localPointB2 - localPointB1;
                _axis = new Vector2(a.Y, -a.X);
                _axis.Normalize();
                var normal = MathUtils.Multiply(ref xfB.R, _axis);

                _localPoint = 0.5f * (localPointB1 + localPointB2);
                var pointB = MathUtils.Multiply(ref xfB, _localPoint);

                var localPointA = proxyA.Vertices[cache.IndexA[0]];
                var pointA = MathUtils.Multiply(ref xfA, localPointA);

                if (Vector2.Dot((Vector2)(pointA - pointB), normal) < 0.0f)
                {
                    _axis = -_axis;
                }
            }
            else
            {
                // Two points on A and one or two points on B.
                _type = SeparationFunctionType.FaceA;
                var localPointA1 = _proxyA.Vertices[cache.IndexA[0]];
                var localPointA2 = _proxyA.Vertices[cache.IndexA[1]];

                var a = localPointA2 - localPointA1;
                _axis = new Vector2(a.Y, -a.X);
                _axis.Normalize();
                var normal = MathUtils.Multiply(ref xfA.R, _axis);

                _localPoint = 0.5f * (localPointA1 + localPointA2);
                var pointA = MathUtils.Multiply(ref xfA, _localPoint);

                var localPointB = _proxyB.Vertices[cache.IndexB[0]];
                var pointB = MathUtils.Multiply(ref xfB, localPointB);

                if (Vector2.Dot((Vector2)(pointB - pointA), normal) < 0.0f)
                {
                    _axis = -_axis;
                }
            }
        }

        public static float FindMinSeparation(out int indexA, out int indexB, float t)
        {
            Transform xfA, xfB;
            _sweepA.GetTransform(out xfA, t);
            _sweepB.GetTransform(out xfB, t);

            switch (_type)
            {
                case SeparationFunctionType.Points:
                {
                    var axisA = MathUtils.MultiplyT(ref xfA.R, _axis);
                    var axisB = MathUtils.MultiplyT(ref xfB.R, -_axis);

                    indexA = _proxyA.GetSupport(axisA);
                    indexB = _proxyB.GetSupport(axisB);

                    var localPointA = _proxyA.Vertices[indexA];
                    var localPointB = _proxyB.Vertices[indexB];

                    var pointA = MathUtils.Multiply(ref xfA, localPointA);
                    var pointB = MathUtils.Multiply(ref xfB, localPointB);

                    var separation = Vector2.Dot((Vector2)(pointB - pointA), _axis);
                    return separation;
                }

                case SeparationFunctionType.FaceA:
                {
                    var normal = MathUtils.Multiply(ref xfA.R, _axis);
                    var pointA = MathUtils.Multiply(ref xfA, _localPoint);

                    var axisB = MathUtils.MultiplyT(ref xfB.R, -normal);

                    indexA = -1;
                    indexB = _proxyB.GetSupport(axisB);

                    var localPointB = _proxyB.Vertices[indexB];
                    var pointB = MathUtils.Multiply(ref xfB, localPointB);

                    var separation = Vector2.Dot((Vector2)(pointB - pointA), normal);
                    return separation;
                }

                case SeparationFunctionType.FaceB:
                {
                    var normal = MathUtils.Multiply(ref xfB.R, _axis);
                    var pointB = MathUtils.Multiply(ref xfB, _localPoint);

                    var axisA = MathUtils.MultiplyT(ref xfA.R, -normal);

                    indexB = -1;
                    indexA = _proxyA.GetSupport(axisA);

                    var localPointA = _proxyA.Vertices[indexA];
                    var pointA = MathUtils.Multiply(ref xfA, localPointA);

                    var separation = Vector2.Dot((Vector2)(pointA - pointB), normal);
                    return separation;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static float Evaluate(int indexA, int indexB, float t)
        {
            Transform xfA, xfB;
            _sweepA.GetTransform(out xfA, t);
            _sweepB.GetTransform(out xfB, t);

            switch (_type)
            {
                case SeparationFunctionType.Points:
                    {
                        Vector2 axisA = MathUtils.MultiplyT(ref xfA.R, _axis);
                        Vector2 axisB = MathUtils.MultiplyT(ref xfB.R, -_axis);

                        Vector2 localPointA = _proxyA.Vertices[indexA];
                        Vector2 localPointB = _proxyB.Vertices[indexB];

                        WorldVector2 pointA = MathUtils.Multiply(ref xfA, localPointA);
                        WorldVector2 pointB = MathUtils.Multiply(ref xfB, localPointB);
                        float separation = Vector2.Dot((Vector2)(pointB - pointA), _axis);

                        return separation;
                    }

                case SeparationFunctionType.FaceA:
                    {
                        Vector2 normal = MathUtils.Multiply(ref xfA.R, _axis);
                        WorldVector2 pointA = MathUtils.Multiply(ref xfA, _localPoint);

                        Vector2 axisB = MathUtils.MultiplyT(ref xfB.R, -normal);

                        Vector2 localPointB = _proxyB.Vertices[indexB];
                        WorldVector2 pointB = MathUtils.Multiply(ref xfB, localPointB);

                        float separation = Vector2.Dot((Vector2)(pointB - pointA), normal);
                        return separation;
                    }

                case SeparationFunctionType.FaceB:
                    {
                        Vector2 normal = MathUtils.Multiply(ref xfB.R, _axis);
                        WorldVector2 pointB = MathUtils.Multiply(ref xfB, _localPoint);

                        Vector2 axisA = MathUtils.MultiplyT(ref xfA.R, -normal);

                        Vector2 localPointA = _proxyA.Vertices[indexA];
                        WorldVector2 pointA = MathUtils.Multiply(ref xfA, localPointA);

                        float separation = Vector2.Dot((Vector2)(pointA - pointB), normal);
                        return separation;
                    }

                default:
                    Debug.Assert(false);
                    return 0.0f;
            }
        }
    }

    internal static class TimeOfImpact
    {
        // CCD via the local separating axis method. This seeks progression
        // by computing the largest time at which separation is maintained.
        private static readonly DistanceInput DistanceInput = new DistanceInput();

        /// <summary>
        /// Compute the upper bound on time before two shapes penetrate. Time is represented as
        /// a fraction between [0,tMax]. This uses a swept separating axis and may miss some intermediate,
        /// non-tunneling collision. If you change the time interval, you should call this function
        /// again.
        /// Note: use Distance() to compute the contact point and normal at the time of impact.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="input">The input.</param>
        public static void CalculateTimeOfImpact(out TOIOutput output, TOIInput input)
        {
            const int kMaxIterations = 20;

            output = new TOIOutput();
            output.State = TOIOutputState.Unknown;
            output.T = input.TMax;

            var sweepA = input.SweepA;
            var sweepB = input.SweepB;

            // Large rotations can make the root finder fail, so we normalize the
            // sweep angles.
            sweepA.Normalize();
            sweepB.Normalize();

            var tMax = input.TMax;

            var totalRadius = input.ProxyA.Radius + input.ProxyB.Radius;
            var target = Math.Max(Settings.LinearSlop, totalRadius - 3.0f * Settings.LinearSlop);
            const float tolerance = 0.25f * Settings.LinearSlop;
            Debug.Assert(target > tolerance);

            var t1 = 0.0f;
            var iter = 0;

            // Prepare input for distance query.
            DistanceInput.ProxyA = input.ProxyA;
            DistanceInput.ProxyB = input.ProxyB;
            DistanceInput.UseRadii = false;

            // The outer loop progressively attempts to compute new separating axes.
            // This loop terminates when an axis is repeated (no progress is made).
            for (;;)
            {
                Transform xfA, xfB;
                sweepA.GetTransform(out xfA, t1);
                sweepB.GetTransform(out xfB, t1);

                // Get the distance between shapes. We can also use the results
                // to get a separating axis.
                DistanceInput.TransformA = xfA;
                DistanceInput.TransformB = xfB;
                DistanceOutput distanceOutput;
                SimplexCache cache;
                Distance.ComputeDistance(out distanceOutput, out cache, DistanceInput);

                // If the shapes are overlapped, we give up on continuous collision.
                if (distanceOutput.Distance <= 0.0f)
                {
                    // Failure!
                    output.State = TOIOutputState.Overlapped;
                    output.T = 0.0f;
                    break;
                }

                if (distanceOutput.Distance < target + tolerance)
                {
                    // Victory!
                    output.State = TOIOutputState.Touching;
                    output.T = t1;
                    break;
                }

                SeparationFunction.Set(ref cache, input.ProxyA, ref sweepA, input.ProxyB, ref sweepB, t1);

                // Compute the TOI on the separating axis. We do this by successively
                // resolving the deepest point. This loop is bounded by the number of vertices.
                var done = false;
                var t2 = tMax;
                var pushBackIter = 0;
                for (; ; )
                {
                    // Find the deepest point at t2. Store the witness point indices.
                    int indexA, indexB;
                    float s2 = SeparationFunction.FindMinSeparation(out indexA, out indexB, t2);

                    // Is the final configuration separated?
                    if (s2 > target + tolerance)
                    {
                        // Victory!
                        output.State = TOIOutputState.Seperated;
                        output.T = tMax;
                        done = true;
                        break;
                    }

                    // Has the separation reached tolerance?
                    if (s2 > target - tolerance)
                    {
                        // Advance the sweeps
                        t1 = t2;
                        break;
                    }

                    // Compute the initial separation of the witness points.
                    var s1 = SeparationFunction.Evaluate(indexA, indexB, t1);

                    // Check for initial overlap. This might happen if the root finder
                    // runs out of iterations.
                    if (s1 < target - tolerance)
                    {
                        output.State = TOIOutputState.Failed;
                        output.T = t1;
                        done = true;
                        break;
                    }

                    // Check for touching
                    if (s1 <= target + tolerance)
                    {
                        // Victory! t1 should hold the TOI (could be 0.0).
                        output.State = TOIOutputState.Touching;
                        output.T = t1;
                        done = true;
                        break;
                    }

                    // Compute 1D root of: f(x) - target = 0
                    var rootIterCount = 0;
                    var a1 = t1;
                    var a2 = t2;
                    for (;;)
                    {
                        // Use a mix of the secant rule and bisection.
                        float t;
                        if ((rootIterCount & 1) != 0)
                        {
                            // Secant rule to improve convergence.
                            t = a1 + (target - s1) * (a2 - a1) / (s2 - s1);
                        }
                        else
                        {
                            // Bisection to guarantee progress.
                            t = 0.5f * (a1 + a2);
                        }

                        // Bad inputs can cause t to become NaN at some point. Detect it here instead of dealing with
                        // the infinite loops the NaN will cause.
                        Debug.Assert(!float.IsNaN(t));

                        var s = SeparationFunction.Evaluate(indexA, indexB, t);

                        Debug.Assert(!float.IsNaN(s));

                        if (Math.Abs(s - target) < tolerance)
                        {
                            // t2 holds a tentative value for t1
                            t2 = t;
                            break;
                        }

                        // Ensure we continue to bracket the root.
                        if (s > target)
                        {
                            a1 = t;
                            s1 = s;
                        }
                        else
                        {
                            a2 = t;
                            s2 = s;
                        }

                        ++rootIterCount;

                        if (rootIterCount == 50)
                        {
                            break;
                        }
                    }

                    ++pushBackIter;

                    if (pushBackIter == Settings.MaxPolygonVertices)
                    {
                        break;
                    }
                }

                ++iter;

                if (done)
                {
                    break;
                }

                if (iter == kMaxIterations)
                {
                    // Root finder got stuck. Semi-victory.
                    output.State = TOIOutputState.Failed;
                    output.T = t1;
                    break;
                }
            }
        }
    }
}