using Microsoft.Xna.Framework;

namespace Engine.Physics.Intersection
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
        public static bool Test(ref Vector2 Ea, ref Vector2 A0, ref Vector2 A1, ref Vector2 Eb, ref Vector2 B0,
                                ref Vector2 B1)
        {
            var A = new Rectangle((int)A0.X, (int)A0.Y, (int)Ea.X, (int)Ea.Y); //previous state of AABB A
            var B = new Rectangle((int)B0.X, (int)B0.Y, (int)Eb.X, (int)Eb.Y); //previous state of AABB B

            //check if they were overlapping
            // on the previous frame
            if (A.Intersects(B))
            {
                return true;
            }

            var va = A1 - A0; //displacement of A
            var vb = B1 - B0; //displacement of B 

            //the problem is solved in A's frame of reference

            //relative velocity (in normalized time)
            Vector2 v = vb - va;

            //first times of overlap along each axis
            Vector2 u_0 = Vector2.Zero;

            //last times of overlap along each axis
            Vector2 u_1 = Vector2.One;

            //find the possible first and last times
            //of overlap along each axis

            // X
            if (A.Right < B.Left && v.X < 0)
            {
                u_0.X = (A.Right - B.Left) / v.X;
            }
            else if (B.Right < A.Left && v.X > 0)
            {
                u_0.X = (A.Left - B.Right) / v.X;
            }

            if (B.Right > A.Left && v.X < 0)
            {
                u_1.X = (A.Left - B.Right) / v.X;
            }
            else if (A.Right > B.Left && v.X > 0)
            {
                u_1.X = (A.Right - B.Left) / v.X;
            }

            // Y
            if (A.Bottom < B.Top && v.Y < 0)
            {
                u_0.Y = (A.Bottom - B.Top) / v.Y;
            }
            else if (B.Bottom < A.Top && v.Y > 0)
            {
                u_0.Y = (A.Top - B.Bottom) / v.Y;
            }

            if (B.Bottom > A.Top && v.Y < 0)
            {
                u_1.Y = (A.Top - B.Bottom) / v.Y;
            }
            else if (A.Bottom > B.Top && v.Y > 0)
            {
                u_1.Y = (A.Bottom - B.Top) / v.Y;
            }

            //possible first time of overlap
            var u0 = System.Math.Max(u_0.X, u_0.Y); //normalized time of first collision

            //possible last time of overlap
            var u1 = System.Math.Min(u_1.X, u_1.Y); //normalized time of second collision 

            //they could have only collided if
            //the first time of overlap occurred
            //before the last time of overlap
            return u0 <= u1;
        }
    }
}
