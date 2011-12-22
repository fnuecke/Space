using Engine.Math;

namespace Engine.Physics.Intersection
{
    public static class SphereAABBSweep
    {
        /// <summary>
        /// Test for collision between moving box and sphere.
        /// </summary>
        /// <param name="ra">radius of sphere</param>
        /// <param name="A0">previous position of sphere</param>
        /// <param name="A1">current position of sphere</param>
        /// <param name="rb">extents of AABB</param>
        /// <param name="B0">previous position of AABB</param>
        /// <param name="B1">current position of AABB</param>
        /// <returns>true if the objects (did) collide.</returns>
        /// <see cref="http://www.geometrictools.com/LibMathematics/Intersection/Wm5IntrBox2Circle2.cpp"/> 
        public static bool Test(Fixed ra, FPoint A0, FPoint A1, FPoint eb, FPoint B0, FPoint B1)
        {
            // Convert circle center to box coordinates.
            FPoint diff = A1 - (B1 + eb / 2);
            FPoint vel = (B1 - B0) - (A1 - A0);

            if (diff.X < -eb.X)
            {
                if (diff.Y < -eb.Y)
                {
                    // region Rmm
                    return TestVertexRegion(ra, diff.X, diff.Y, vel.X, vel.Y, eb.X, eb.Y);
                }
                else if (diff.Y <= eb.Y)
                {
                    // region Rmz
                    return TestEdgeRegion(ra, diff.X, diff.Y, vel.X, vel.Y, eb.X, eb.Y);
                }
                else
                {
                    // region Rmp
                    return TestVertexRegion(ra, diff.X, -diff.Y, vel.X, -vel.Y, eb.X, eb.Y);
                }
            }
            else if (diff.X <= eb.X)
            {
                if (diff.Y < -eb.Y)
                {
                    // region Rzm
                    return TestEdgeRegion(ra, diff.Y, diff.X, vel.Y, vel.X, eb.Y, eb.X);
                }
                else if (diff.Y <= eb.Y)
                {
                    return true;
                }
                else
                {
                    // region Rzp
                    return TestEdgeRegion(ra, -diff.Y, diff.X, -vel.Y, vel.X, eb.Y, eb.X);
                }
            }
            else
            {
                if (diff.Y < -eb.Y)
                {
                    // region Rpm
                    return TestVertexRegion(ra, -diff.X, diff.Y, -vel.X, vel.Y, eb.X, eb.Y);
                }
                else if (diff.Y <= eb.Y)
                {
                    // region Rpz
                    return TestEdgeRegion(ra, -diff.X, diff.Y, -vel.X, vel.Y, eb.X, eb.Y);
                }
                else
                {
                    // region Rpp
                    return TestVertexRegion(ra, -diff.X, -diff.Y, -vel.X, -vel.Y, eb.X, eb.Y);
                }
            }
        }

        private static bool TestVertexRegion(Fixed ra, Fixed cx, Fixed cy, Fixed vx, Fixed vy, Fixed ex, Fixed ey)
        {
            Fixed dx = cx + ex;
            Fixed dy = cy + ey;
            Fixed rsqr = ra * ra;
            Fixed diff = dx * dx + dy * dy - rsqr;
            if (diff <= (Fixed)0)
            {
                // Circle is already intersecting the box.
                return true;
            }

            Fixed dot = vx * dx + vy * dy;
            if (dot >= (Fixed)0)
            {
                // Circle not moving towards box.
                return false;
            }

            Fixed dotPerp = vx * dy - vy * dx;
            if (dotPerp >= (Fixed)0)
            {
                // Potential contact on left edge.
                if (dotPerp <= ra * vy)
                {
                    // Lower left corner is first point of contact.
                    return true;
                }

                if (vx <= (Fixed)0)
                {
                    // Passed corner, moving away from box.
                    return false;
                }

                Fixed vsqr = vx * vx + vy * vy;
                dy = cy - ey;
                dotPerp = vx * dy - vy * dx;
                if (dotPerp >= (Fixed)0 && dotPerp * dotPerp > rsqr * vsqr)
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

                if (vy <= (Fixed)0)
                {
                    // Passed corner, moving away from box.
                    return false;
                }

                Fixed vsqr = vx * vx + vy * vy;
                dx = cx - ex;
                dotPerp = vx * dy - vy * dx;
                if (-dotPerp >= (Fixed)0 && dotPerp * dotPerp > rsqr * vsqr)
                {
                    // Circle misses box.
                    return false;
                }
            }

            return true;
        }

        private static bool TestEdgeRegion(Fixed ra, Fixed cx, Fixed cy, Fixed vx, Fixed vy, Fixed ex, Fixed ey)
        {
            Fixed dx = cx + ex;
            Fixed xSignedDist = dx + ra;
            if (xSignedDist >= (Fixed)0)
            {
                // Circle is already intersecting the box.
                return true;
            }

            if (vx <= (Fixed)0)
            {
                // Circle not moving towards box.
                return false;
            }

            Fixed rsqr = ra * ra;
            Fixed vsqr = vx * vx + vy * vy;
            if (vy >= (Fixed)0)
            {
                Fixed dy = cy - ey;
                Fixed dotPerp = vx * dy - vy * dx;
                if (dotPerp >= (Fixed)0 && dotPerp * dotPerp > rsqr * vsqr)
                {
                    // Circle misses box.
                    return false;
                }
            }
            else
            {
                Fixed dy = cy + ey;
                Fixed dotPerp = vx * dy - vy * dx;
                if (dotPerp <= (Fixed)0 && dotPerp * dotPerp > rsqr * vsqr)
                {
                    // Circle misses box.
                    return false;
                }
            }

            return true;
        }
    }
}
