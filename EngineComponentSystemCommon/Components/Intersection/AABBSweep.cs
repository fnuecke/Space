using Engine.FarMath;
using Engine.Math;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Components.Intersection
{
    /// <summary>
    /// Performs a sweep test between two AABBs.
    /// </summary>
    public static class AABBSweep
    {
        /// <summary>
        /// Sweep two AABB's to see if and when they first and last were overlapping.
        /// </summary>
        /// <param name="Ea">extents of AABB A</param>
        /// <param name="A0">its previous position</param>
        /// <param name="A1">its current position</param>
        /// <param name="Eb">extents of AABB B</param>
        /// <param name="B0">its previous position</param>
        /// <param name="B1">its current position</param>
        /// <returns>true if the boxes (did) collide.</returns>
        /// <see cref="http://www.gamasutra.com/view/feature/3383/simple_intersection_tests_for_games.php?page=3"/>
        public static bool Test(ref Vector2 Ea, ref FarPosition A0, ref FarPosition A1,
            ref Vector2 Eb, ref FarPosition B0, ref FarPosition B1)
        {
            var a = new RectangleF(0, 0, Ea.X, Ea.Y); //previous state of AABB A in local co-sy
            var relB0 = (Vector2)(B0 - A0); // previous state of AABB B in A's local co-sy
            var b = new RectangleF(relB0.X, relB0.Y, Eb.X, Eb.Y);

            //check if they were overlapping
            // on the previous frame
            if (a.Intersects(b))
            {
                return true;
            }

            var va = (Vector2)(A0 - A1); //displacement of A
            var vb = (Vector2)(B0 - B1); //displacement of B 

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
            if (a.Right < b.Left && v.X < 0)
            {
                v0.X = (a.Right - b.Left) / v.X;
            }
            else if (b.Right < a.Left && v.X > 0)
            {
                v0.X = (a.Left - b.Right) / v.X;
            }

            if (b.Right > a.Left && v.X < 0)
            {
                v1.X = (a.Left - b.Right) / v.X;
            }
            else if (a.Right > b.Left && v.X > 0)
            {
                v1.X = (a.Right - b.Left) / v.X;
            }

            // Y
            if (a.Bottom < b.Top && v.Y < 0)
            {
                v0.Y = (a.Bottom - b.Top) / v.Y;
            }
            else if (b.Bottom < a.Top && v.Y > 0)
            {
                v0.Y = (a.Top - b.Bottom) / v.Y;
            }

            if (b.Bottom > a.Top && v.Y < 0)
            {
                v1.Y = (a.Top - b.Bottom) / v.Y;
            }
            else if (a.Bottom > b.Top && v.Y > 0)
            {
                v1.Y = (a.Bottom - b.Top) / v.Y;
            }

            //possible first time of overlap
            var u0 = System.Math.Max(v0.X, v0.Y); //normalized time of first collision

            //possible last time of overlap
            var u1 = System.Math.Min(v1.X, v1.Y); //normalized time of second collision 

            //they could have only collided if
            //the first time of overlap occurred
            //before the last time of overlap
            return u0 <= u1;
        }
    }
}
