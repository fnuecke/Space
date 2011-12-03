using Engine.Math;

namespace Engine.Physics.Intersection
{
    internal sealed class AABBSweep
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
        public static bool Test(ref FPoint Ea, ref FPoint A0, ref FPoint A1, ref FPoint Eb, ref FPoint B0, ref FPoint B1)
        {
            FRectangle A = FRectangle.Create(A0, Ea.X, Ea.Y);//previous state of AABB A
            FRectangle B = FRectangle.Create(B0, Eb.X, Eb.Y);//previous state of AABB B

            //check if they were overlapping
            // on the previous frame
            if (A.Intersects(B))
            {
                return true;
            }

            FPoint va = A1 - A0; //displacement of A
            FPoint vb = B1 - B0; //displacement of B 

            //the problem is solved in A's frame of reference

            //relative velocity (in normalized time)
            FPoint v = vb - va;

            //first times of overlap along each axis
            FPoint u_0 = FPoint.Create((Fixed)0, (Fixed)0);

            //last times of overlap along each axis
            FPoint u_1 = FPoint.Create((Fixed)1, (Fixed)1);

            //find the possible first and last times
            //of overlap along each axis

            // X
            if (A.BottomRight.X < B.TopLeft.X && v.X < 0)
            {
                u_0.X = (A.BottomRight.X - B.TopLeft.X) / v.X;
            }
            else if (B.BottomRight.X < A.TopLeft.X && v.X > 0)
            {
                u_0.X = (A.TopLeft.X - B.BottomRight.X) / v.X;
            }

            if (B.BottomRight.X > A.TopLeft.X && v.X < 0)
            {
                u_1.X = (A.TopLeft.X - B.BottomRight.X) / v.X;
            }
            else if (A.BottomRight.X > B.TopLeft.X && v.X > 0)
            {
                u_1.X = (A.BottomRight.X - B.TopLeft.X) / v.X;
            }

            // Y
            if (A.BottomRight.Y < B.TopLeft.Y && v.Y < 0)
            {
                u_0.Y = (A.BottomRight.Y - B.TopLeft.Y) / v.Y;
            }
            else if (B.BottomRight.Y < A.TopLeft.Y && v.Y > 0)
            {
                u_0.Y = (A.TopLeft.Y - B.BottomRight.Y) / v.Y;
            }

            if (B.BottomRight.Y > A.TopLeft.Y && v.Y < 0)
            {
                u_1.Y = (A.TopLeft.Y - B.BottomRight.Y) / v.Y;
            }
            else if (A.BottomRight.Y > B.TopLeft.Y && v.Y > 0)
            {
                u_1.Y = (A.BottomRight.Y - B.TopLeft.Y) / v.Y;
            }

            Fixed u0; //normalized time of first collision
            Fixed u1; //normalized time of second collision 

            //possible first time of overlap
            u0 = Fixed.Max(u_0.X, u_0.Y);

            //possible last time of overlap
            u1 = Fixed.Min(u_1.X, u_1.Y);

            //they could have only collided if
            //the first time of overlap occurred
            //before the last time of overlap
            return u0 <= u1;
        }

        private AABBSweep()
        {
        }
    }
}
