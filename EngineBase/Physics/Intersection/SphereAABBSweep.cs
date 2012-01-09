using Microsoft.Xna.Framework;

namespace Engine.Physics.Intersection
{
    /// <summary>
    /// Performs a sweep test for a sphere and an AABB.
    /// </summary>
    public static class SphereAABBSweep
    {
        /// <summary>
        /// Test for collision between moving box and sphere.
        /// </summary>
        /// <remarks>
        /// See http://www.geometrictools.com/LibMathematics/Intersection/Wm5IntrBox2Circle2.cpp"
        /// </remarks>
        /// <param name="ra">radius of sphere</param>
        /// <param name="A0">previous position of sphere</param>
        /// <param name="A1">current position of sphere</param>
        /// <param name="eb">extents of AABB</param>
        /// <param name="B0">previous position of AABB</param>
        /// <param name="B1">current position of AABB</param>
        /// <returns>true if the objects (did) collide.</returns>
        public static bool Test(float ra, ref Vector2 A0, ref Vector2 A1, ref Vector2 eb, ref Vector2 B0, ref Vector2 B1)
        {
            // Convert circle center to box coordinates.
            Vector2 diff = A1 - (B1 + eb / 2);
            Vector2 vel = (B1 - B0) - (A1 - A0);

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

        private static bool TestVertexRegion(float ra, float cx, float cy, float vx, float vy, float ex, float ey)
        {
            float dx = cx + ex;
            float dy = cy + ey;
            float rsqr = ra * ra;
            float diff = dx * dx + dy * dy - rsqr;
            if (diff <= 0)
            {
                // Circle is already intersecting the box.
                return true;
            }

            float dot = vx * dx + vy * dy;
            if (dot >= 0)
            {
                // Circle not moving towards box.
                return false;
            }

            float dotPerp = vx * dy - vy * dx;
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

                float vsqr = vx * vx + vy * vy;
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

                float vsqr = vx * vx + vy * vy;
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
            float dx = cx + ex;
            float xSignedDist = dx + ra;
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

            float rsqr = ra * ra;
            float vsqr = vx * vx + vy * vy;
            if (vy >= 0)
            {
                float dy = cy - ey;
                float dotPerp = vx * dy - vy * dx;
                if (dotPerp >= 0 && dotPerp * dotPerp > rsqr * vsqr)
                {
                    // Circle misses box.
                    return false;
                }
            }
            else
            {
                float dy = cy + ey;
                float dotPerp = vx * dy - vy * dx;
                if (dotPerp <= 0 && dotPerp * dotPerp > rsqr * vsqr)
                {
                    // Circle misses box.
                    return false;
                }
            }

            return true;
        }
    }
}
