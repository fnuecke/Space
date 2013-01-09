using System;
using Engine.FarMath;
using Engine.Math;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>Contains methods for intersection computation between various shapes.</summary>
    internal static class Intersection
    {
        #region Simple tests

        /// <summary>Tests if two rectangles, defined by the specified centers and sizes, intersect.</summary>
        /// <param name="sizeA">The size of the first rectangle.</param>
        /// <param name="positionA">The center of the first rectangle.</param>
        /// <param name="sizeB">The size of the second rectangle.</param>
        /// <param name="positionB">The center of the second rectangle.</param>
        /// <returns></returns>
        public static bool Test(
            ref Vector2 sizeA,
            ref FarPosition positionA,
            ref Vector2 sizeB,
            ref FarPosition positionB)
        {
            // Move second rectangle to local coordinate system of first rectangle and
            // do a normal float check, for faster computation.
            RectangleF localA;
            localA.X = 0;
            localA.Y = 0;
            localA.Width = sizeA.X;
            localA.Height = sizeA.Y;
            RectangleF localB;
            localB.X = (float) (positionB.X - positionA.X) + (sizeA.X - sizeB.X) * 0.5f;
            localB.Y = (float) (positionB.Y - positionA.Y) + (sizeA.Y - sizeB.Y) * 0.5f;
            localB.Width = sizeB.X;
            localB.Height = sizeB.Y;

            return localA.Intersects(localB);
        }

        /// <summary>Tests if two circles intersect.</summary>
        /// <param name="radiusA">The radius of the first circle.</param>
        /// <param name="positionA">The center of the first circle.</param>
        /// <param name="radiusB">The radius of the second circle.</param>
        /// <param name="positionB">The center of the second circle.</param>
        /// <returns></returns>
        public static bool Test(
            float radiusA,
            ref FarPosition positionA,
            float radiusB,
            ref FarPosition positionB)
        {
            var d = (Vector2) (positionB - positionA);
            var r = radiusA + radiusB;
            return (d.X * d.X + d.Y * d.Y) <= r * r;
        }

        /// <summary>Tests if a sphere and a rectangle overlap.</summary>
        /// <param name="sizeA">The size of the rectangle.</param>
        /// <param name="positionA">The center of the rectangle.</param>
        /// <param name="radiusB">The radius of the circle.</param>
        /// <param name="positionB">The center the circle.</param>
        /// <returns></returns>
        public static bool Test(
            ref Vector2 sizeA,
            ref FarPosition positionA,
            float radiusB,
            ref FarPosition positionB)
        {
            // Translate rectangle to circle's local coordinate system and do normal
            // float check for faster computation.
            var localALeft = (float) (positionA.X - positionB.X) - sizeA.X * 0.5f;
            var localATop = (float) (positionA.Y - positionB.Y) - sizeA.Y * 0.5f;
            var localARight = localALeft + sizeA.X;
            var localABottom = localATop + sizeA.Y;

            // Check for unaligned separation.
            var closest = Vector2.Zero;
            if (closest.X < localALeft)
            {
                closest.X = localALeft;
            }
            else if (closest.X > localARight)
            {
                closest.X = localARight;
            }
            if (closest.Y < localATop)
            {
                closest.Y = localATop;
            }
            else if (closest.Y > localABottom)
            {
                closest.Y = localABottom;
            }

            // Got a closest point, check the distance.
            return (closest.X * closest.X + closest.Y * closest.Y) <= radiusB * radiusB;
        }

        #endregion

        #region Sweeping tests

        /// <summary>Sweep two AABB's to see if and when they first and last were overlapping.</summary>
        /// <param name="sizeA">extents of first rectangle.</param>
        /// <param name="positionA0">previous center of first rectangle.</param>
        /// <param name="positionA1">current center of first rectangle.</param>
        /// <param name="sizeB">extents of second rectangle.</param>
        /// <param name="positionB0">previous center of second rectangle.</param>
        /// <param name="positionB1">current center of second rectangle.</param>
        /// <param name="t">The relative time between previous and current position at which the rectangles intersected.</param>
        /// <returns>true if the boxes (did) collide.</returns>
        /// <see cref="http://www.gamasutra.com/view/feature/3383/simple_intersection_tests_for_games.php?page=3"/>
        public static bool Test(
            ref Vector2 sizeA,
            ref FarPosition positionA0,
            ref FarPosition positionA1,
            ref Vector2 sizeB,
            ref FarPosition positionB0,
            ref FarPosition positionB1,
            out float t)
        {
            // previous state of AABB A in local co-sy
            var localARight = sizeA.X;
            var localABottom = sizeA.Y;
            // previous state of AABB B in A's local co-sy
            var localBLeft = (float) (positionB0.X - positionA0.X) + (sizeA.X - sizeB.X) * 0.5f;
            var localBTop = (float) (positionB0.Y - positionA0.Y) + (sizeA.Y - sizeB.Y) * 0.5f;
            var localBRight = localBLeft + sizeB.X;
            var localBBottom = localBTop + sizeB.Y;

            // check if they were overlapping
            // on the previous frame
            if (localBRight > 0 &&
                localBLeft < localARight &&
                localBBottom > 0 &&
                localBTop < localABottom)
            {
                t = 0;
                return true;
            }

            //displacement of A
            var va = (Vector2) (positionA0 - positionA1);
            //displacement of B
            var vb = (Vector2) (positionB0 - positionB1);

            //the problem is solved in A's frame of reference

            //relative velocity (in normalized time)
            var v = vb - va;

            //first times of overlap along each axis
            var v0 = Vector2.Zero;

            //last times of overlap along each axis
            var v1 = Vector2.One;

            //find the possible first and last times
            //of overlap along each axis

            // X
            if (localARight < localBLeft && v.X < 0)
            {
                v0.X = (localARight - localBLeft) / v.X;
            }
            else if (localBRight < 0 && v.X > 0)
            {
                v0.X = -localBRight / v.X;
            }

            if (localBRight > 0 && v.X < 0)
            {
                v1.X = -localBRight / v.X;
            }
            else if (localARight > localBLeft && v.X > 0)
            {
                v1.X = (localARight - localBLeft) / v.X;
            }

            // Y
            if (localABottom < localBTop && v.Y < 0)
            {
                v0.Y = (localABottom - localBTop) / v.Y;
            }
            else if (localBBottom < 0 && v.Y > 0)
            {
                v0.Y = -localBBottom / v.Y;
            }

            if (localBBottom > 0 && v.Y < 0)
            {
                v1.Y = -localBBottom / v.Y;
            }
            else if (localABottom > localBTop && v.Y > 0)
            {
                v1.Y = (localABottom - localBTop) / v.Y;
            }

            //possible first time of overlap
            var u0 = System.Math.Max(v0.X, v0.Y); //normalized time of first collision

            //possible last time of overlap
            var u1 = System.Math.Min(v1.X, v1.Y); //normalized time of second collision 

            //they could have only collided if
            //the first time of overlap occurred
            //before the last time of overlap
            t = u0;
            return u0 <= u1;
        }

        /// <summary>Test for collision between two moving spheres.</summary>
        /// <param name="radiusA">radius of first sphere.</param>
        /// <param name="positionA0">previous center of first sphere.</param>
        /// <param name="positionA1">current center of first sphere.</param>
        /// <param name="radiusB">radius of second sphere.</param>
        /// <param name="positionB0">previous center of second sphere.</param>
        /// <param name="positionB1">current center of second sphere.</param>
        /// <param name="t">The relative time between previous and current position at which the spheres intersected.</param>
        /// <returns>true if the spheres (did) collide.</returns>
        /// <see cref="http://www.gamasutra.com/view/feature/3383/simple_intersection_tests_for_games.php?page=2"/>
        public static bool Test(
            float radiusA,
            ref FarPosition positionA0,
            ref FarPosition positionA1,
            float radiusB,
            ref FarPosition positionB0,
            ref FarPosition positionB1,
            out float t)
        {
            //displacement of A
            var va = (Vector2) (positionA1 - positionA0);
            //displacement of B
            var vb = (Vector2) (positionB1 - positionB0);

            // relative position of B to A
            var ab = (Vector2) (positionB0 - positionA0);

            // relative velocity (in normalized time)
            var vab = vb - va;
            var rab = radiusA + radiusB;

            // u*u coefficient
            var a = Vector2.Dot(vab, vab);

            // u coefficient
            var b = -2f * Vector2.Dot(vab, ab);

            // constant term 
            var c = Vector2.Dot(ab, ab) - rab * rab;

            //check if they're currently overlapping
            if (Vector2.Dot(ab, ab) <= rab * rab)
            {
                t = 0;
                return true;
            }

            // check if they hit each other
            // during the frame
            var q = b * b - 4 * a * c;
            if (q < 0)
            {
                t = 0;
                return false;
            }

            var sq = (float) System.Math.Sqrt(q);
            var d = 1f / (a + a);
            var r1 = (b + sq) * d;
            var r2 = (b - sq) * d;

            t = r2 > r1 ? r2 : r1;

            return t >= 0f && t <= 1f;
        }

        /// <summary>Test for collision between moving box and sphere.</summary>
        /// <param name="radiusA">radius of the sphere.</param>
        /// <param name="positionA0">previous center of the sphere.</param>
        /// <param name="positionA1">current center of the sphere.</param>
        /// <param name="sizeB">extents of the AABB.</param>
        /// <param name="positionB0">previous center of the AABB.</param>
        /// <param name="positionB1">current center of the AABB.</param>
        /// <param name="t">The relative time between previous and current positions at which the sphere and box intersected.</param>
        /// <returns>true if the objects (did) collide.</returns>
        /// <remarks>See http://www.geometrictools.com/LibMathematics/Intersection/Wm5IntrBox2Circle2.cpp"</remarks>
        public static bool Test(
            float radiusA,
            ref FarPosition positionA0,
            ref FarPosition positionA1,
            ref Vector2 sizeB,
            ref FarPosition positionB0,
            ref FarPosition positionB1,
            out float t)
        {
            t = 0;
            throw new NotImplementedException(); // TODO return intersection time and re-check code in general

            // Convert circle center to box coordinates.
            var diff = (Vector2) (positionA1 - positionB1) - (sizeB / 2);
            var vel = (Vector2) (positionB1 - positionB0) - (Vector2) (positionA1 - positionA0);

            if (diff.X < -sizeB.X)
            {
                if (diff.Y < -sizeB.Y)
                {
                    // region Rmm
                    return TestVertexRegion(radiusA, diff.X, diff.Y, vel.X, vel.Y, sizeB.X, sizeB.Y);
                }
                else if (diff.Y <= sizeB.Y)
                {
                    // region Rmz
                    return TestEdgeRegion(radiusA, diff.X, diff.Y, vel.X, vel.Y, sizeB.X, sizeB.Y);
                }
                else
                {
                    // region Rmp
                    return TestVertexRegion(radiusA, diff.X, -diff.Y, vel.X, -vel.Y, sizeB.X, sizeB.Y);
                }
            }
            else if (diff.X <= sizeB.X)
            {
                if (diff.Y < -sizeB.Y)
                {
                    // region Rzm
                    return TestEdgeRegion(radiusA, diff.Y, diff.X, vel.Y, vel.X, sizeB.Y, sizeB.X);
                }
                else if (diff.Y <= sizeB.Y)
                {
                    return true;
                }
                else
                {
                    // region Rzp
                    return TestEdgeRegion(radiusA, -diff.Y, diff.X, -vel.Y, vel.X, sizeB.Y, sizeB.X);
                }
            }
            else
            {
                if (diff.Y < -sizeB.Y)
                {
                    // region Rpm
                    return TestVertexRegion(radiusA, -diff.X, diff.Y, -vel.X, vel.Y, sizeB.X, sizeB.Y);
                }
                else if (diff.Y <= sizeB.Y)
                {
                    // region Rpz
                    return TestEdgeRegion(radiusA, -diff.X, diff.Y, -vel.X, vel.Y, sizeB.X, sizeB.Y);
                }
                else
                {
                    // region Rpp
                    return TestVertexRegion(radiusA, -diff.X, -diff.Y, -vel.X, -vel.Y, sizeB.X, sizeB.Y);
                }
            }
        }

        private static bool TestVertexRegion(float ra, float cx, float cy, float vx, float vy, float ex, float ey)
        {
            var dx = cx + ex;
            var dy = cy + ey;
            var rsqr = ra * ra;
            var diff = dx * dx + dy * dy - rsqr;
            if (diff <= 0)
            {
                // Circle is already intersecting the box.
                return true;
            }

            var dot = vx * dx + vy * dy;
            if (dot >= 0)
            {
                // Circle not moving towards box.
                return false;
            }

            var dotPerp = vx * dy - vy * dx;
            if (dotPerp >= 0)
            {
                // Potential contact on left edge.
                if (dotPerp <= ra * vy)
                {
                    // Lower left corner is first point of contact.
                    return true;
                }

                if (vx <= 0)
                {
                    // Passed corner, moving away from box.
                    return false;
                }

                var vsqr = vx * vx + vy * vy;
                dy = cy - ey;
                dotPerp = vx * dy - vy * dx;
                if (dotPerp >= 0 && dotPerp * dotPerp > rsqr * vsqr)
                {
                    // Circle misses box.
                    return false;
                }
            }
            else
            {
                // Potential contact on bottom edge.
                if (-dotPerp <= ra * vx)
                {
                    // Lower left corner is first point of contact.
                    return true;
                }

                if (vy <= 0)
                {
                    // Passed corner, moving away from box.
                    return false;
                }

                var vsqr = vx * vx + vy * vy;
                dx = cx - ex;
                dotPerp = vx * dy - vy * dx;
                if (-dotPerp >= 0 && dotPerp * dotPerp > rsqr * vsqr)
                {
                    // Circle misses box.
                    return false;
                }
            }

            return true;
        }

        private static bool TestEdgeRegion(float ra, float cx, float cy, float vx, float vy, float ex, float ey)
        {
            var dx = cx + ex;
            var xSignedDist = dx + ra;
            if (xSignedDist >= 0)
            {
                // Circle is already intersecting the box.
                return true;
            }

            if (vx <= 0)
            {
                // Circle not moving towards box.
                return false;
            }

            var rsqr = ra * ra;
            var vsqr = vx * vx + vy * vy;
            if (vy >= 0)
            {
                var dy = cy - ey;
                var dotPerp = vx * dy - vy * dx;
                if (dotPerp >= 0 && dotPerp * dotPerp > rsqr * vsqr)
                {
                    // Circle misses box.
                    return false;
                }
            }
            else
            {
                var dy = cy + ey;
                var dotPerp = vx * dy - vy * dx;
                if (dotPerp <= 0 && dotPerp * dotPerp > rsqr * vsqr)
                {
                    // Circle misses box.
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}