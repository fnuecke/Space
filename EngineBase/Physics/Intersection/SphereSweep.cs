using Engine.Math;

namespace Engine.Physics.Intersection
{
    public static class SphereSweep
    {
        /// <summary>
        /// Test for collision between two moving spheres.
        /// </summary>
        /// <param name="ra">radius of sphere A</param>
        /// <param name="A0">previous position of sphere A</param>
        /// <param name="A1">current position of sphere A</param>
        /// <param name="rb">radius of sphere B</param>
        /// <param name="B0">previous position of sphere B</param>
        /// <param name="B1">current position of sphere B</param>
        /// <returns>true if the spheres (did) collide.</returns>
        /// <see cref="http://www.gamasutra.com/view/feature/3383/simple_intersection_tests_for_games.php?page=2"/> 
        public static bool Test(Fixed ra, FPoint A0, FPoint A1, Fixed rb, FPoint B0, FPoint B1)
        {
            FPoint va = A1 - A0;
            FPoint vb = B1 - B0;
            FPoint AB = B0 - A0;

            //relative velocity (in normalized time)
            FPoint vab = vb - va;
            Fixed rab = ra + rb;

            //u*u coefficient
            Fixed a = FPoint.Dot(vab, vab);

            //u coefficient
            Fixed b = 2 * FPoint.Dot(vab, AB);

            //constant term 
            Fixed c = FPoint.Dot(AB, AB) - rab * rab;

            //check if they're currently overlapping
            if (FPoint.Dot(AB, AB) <= rab * rab)
            {
                return true;
            }
            else if (va.Norm == 0 && vb.Norm == 0)
            {
                // neither is moving, cannot collide.
                return false;
            }

            //check if they hit each other
            // during the frame
            var q = b * b - 4 * a * c;
            if (q >= 0)
            {
                q = Fixed.Sqrt(q);
                Fixed d = (Fixed)1 / (a + a);
                if (d > 0)
                {
                    var r1 = (-b + q) * d;
                    var r2 = (-b - q) * d;
                    if (r2 > r1)
                    {
                        r1 = r2;
                    }
                    if (r1 >= 0 && r1 <= 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
