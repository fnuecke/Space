// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
#if FARMATH
using Engine.Collections;
using TPoint = Engine.FarMath.FarPosition;
using TSingle = Engine.FarMath.FarValue;
using TRectangle = Engine.FarMath.FarRectangle;
#else
using TPoint = Microsoft.Xna.Framework.Vector2;
using TSingle = System.Single;
using TRectangle = Engine.Math.RectangleF;
#endif

#if FARMATH
namespace Engine.FarCollections
#else
namespace Engine.Collections
#endif
{
    /// <summary>Utility methods for intersection operations of index structures.</summary>
    public static class IntersectionExtensions
    {
        /// <summary>Compute minimal bounding rectangle around a circle.</summary>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        public static TRectangle BoundsFor(TPoint center, float radius)
        {
            TRectangle rectangle;
            rectangle.X = center.X - radius;
            rectangle.Y = center.Y - radius;
            rectangle.Width = rectangle.Height = radius + radius;
            return rectangle;
        }

        /// <summary>Compute minimal bounding rectangle around line.</summary>
        /// <param name="a">Start of the line.</param>
        /// <param name="b">End of the line.</param>
        /// <param name="t">The fraction of the line to bound.</param>
        /// <returns></returns>
        public static TRectangle BoundsFor(TPoint a, TPoint b, float t)
        {
// ReSharper disable RedundantCast Necessary for FarCollections.
            TRectangle rectangle;
            var c = a + (Microsoft.Xna.Framework.Vector2) (b - a) * t;
            if (a.X < c.X)
            {
                rectangle.X = a.X;
                rectangle.Width = (float) (c.X - a.X);
            }
            else
            {
                rectangle.X = c.X;
                rectangle.Width = (float) (a.X - c.X);
            }
            if (a.Y < c.Y)
            {
                rectangle.Y = a.Y;
                rectangle.Height = (float) (c.Y - a.Y);
            }
            else
            {
                rectangle.Y = c.Y;
                rectangle.Height = (float) (a.Y - c.Y);
            }
            return rectangle;
// ReSharper restore RedundantCast
        }

        /// <summary>Test for intersection between two rectangles.</summary>
        /// <param name="a">First rectangle.</param>
        /// <param name="b">Second rectangle.</param>
        /// <returns></returns>
        public static bool Intersects(this TRectangle a, TRectangle b)
        {
#if FARMATH
            // Move second rectangle to local coordinate system of first rectangle and
            // do a normal float check, for faster computation.
            Math.RectangleF la;
            la.X = 0;
            la.Y = 0;
            la.Width = a.Width;
            la.Height = a.Height;
            Math.RectangleF lb;
            lb.X = (float)(b.X - a.X);
            lb.Y = (float)(b.Y - a.Y);
            lb.Width = b.Width;
            lb.Height = b.Height;
            return la.Intersects(lb);
#else
            var aLeft = a.X;
            var aTop = a.Y;
            var aRight = aLeft + a.Width;
            var aBottom = aTop + a.Height;

            var bLeft = b.X;
            var bTop = b.Y;
            var bRight = bLeft + b.Width;
            var bBottom = bTop + b.Height;

            return aLeft <= bRight &&
                   aTop <= bBottom &&
                   aRight >= bLeft &&
                   aBottom >= bTop;
#endif
        }

        /// <summary>Test for intersection between rectangle and circle.</summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns></returns>
        /// <remarks>
        ///     This method assumes the bounds of the circle intersect the rectangle. It will still work otherwise, but it
        ///     takes no steps to optimize in that regard, as it's normally assumed this is known and thus would be unnecessary
        ///     work to check for.
        /// </remarks>
        public static bool Intersects(this TRectangle rectangle, TPoint center, float radius)
        {
#if FARMATH
            // Translate rectangle to circle's local coordinate system and do normal
            // float check for faster computation.
            Math.RectangleF translated;
            translated.X = (float)(rectangle.X - center.X);
            translated.Y = (float)(rectangle.Y - center.Y);
            translated.Width = (float)rectangle.Width;
            translated.Height = (float)rectangle.Height;
            var zero = Microsoft.Xna.Framework.Vector2.Zero;
            return translated.Intersects(zero, radius);
#else
            var boundsLeft = rectangle.X;
            var boundsTop = rectangle.Y;
            var boundsRight = boundsLeft + rectangle.Width;
            var boundsBottom = boundsTop + rectangle.Height;

            // Check for unaligned separation.
            var closest = center;
            if (closest.X < boundsLeft)
            {
                closest.X = boundsLeft;
            }
            else if (closest.X > boundsRight)
            {
                closest.X = boundsRight;
            }
            if (closest.Y < boundsTop)
            {
                closest.Y = boundsTop;
            }
            else if (closest.Y > boundsBottom)
            {
                closest.Y = boundsBottom;
            }

            // Got a closest point, check the distance.
            var distanceX = closest.X - center.X;
            var distanceY = closest.Y - center.Y;
            return (distanceX * distanceX + distanceY * distanceY) <= radius * radius;
#endif
        }

        /// <summary>Test for intersection between a rectangle and a line.</summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <param name="a">Start of the line.</param>
        /// <param name="b">End of the line.</param>
        /// <param name="t">The fraction of the line to consider.</param>
        /// <param name="tHit">The fraction at which the intersection occurred.</param>
        /// <returns></returns>
        public static bool Intersects(this TRectangle rectangle, TPoint a, TPoint b, float t, out float tHit)
        {
#if FARMATH
            // Move points to local coordinate system of rectangle and do normal float
            // check for faster computation.
            Math.RectangleF lr;
            lr.X = 0;
            lr.Y = 0;
            lr.Width = (float)rectangle.Width;
            lr.Height = (float)rectangle.Height;
            Microsoft.Xna.Framework.Vector2 la;
            la.X = (float)(a.X - rectangle.X);
            la.Y = (float)(a.Y - rectangle.Y);
            Microsoft.Xna.Framework.Vector2 lb;
            lb.X = (float)(b.X - rectangle.X);
            lb.Y = (float)(b.Y - rectangle.Y);
            return lr.Intersects(la, lb, t, out tHit);
#else
            const float e = 1.192092896e-07f;

            tHit = 0f;
            var tMin = float.MinValue;
            var tMax = float.MaxValue;

            var left = rectangle.X;
            var right = left + rectangle.Width;

// ReSharper disable RedundantCast Necessary for FarCollections.
            var dx = (float) (b.X - a.X);
// ReSharper restore RedundantCast
            var parallelToYAxis = dx >= 0 ? (dx < e) : (dx > -e);

            if (parallelToYAxis)
            {
                // Parallel along the x axis, see if we're left or right of the rectangle.
                if (a.X < left || a.X > right)
                {
                    // Outside the box.
                    return false;
                }
            }
            else
            {
                var inverseDistanceX = 1f / dx;
// ReSharper disable RedundantCast Necessary for FarCollections.
                var t1 = (float) (left - a.X) * inverseDistanceX;
                var t2 = (float) (right - a.X) * inverseDistanceX;
// ReSharper restore RedundantCast

                if (t1 > t2)
                {
                    var tmp = t1;
                    t1 = t2;
                    t2 = tmp;
                }

                // Push the min up.
                if (t1 > tMin)
                {
                    tMin = t1;
                }

                // Pull the max down.
                if (t2 < tMax)
                {
                    tMax = t2;
                }

                if (tMin > tMax)
                {
                    return false;
                }
            }

            var top = rectangle.Y;
            var bottom = top + rectangle.Height;

// ReSharper disable RedundantCast Necessary for FarCollections.
            var dy = (float) (b.Y - a.Y);
// ReSharper restore RedundantCast
            var parallelToXAxis = dy >= 0 ? (dy < e) : (dy > -e);

            if (parallelToXAxis)
            {
                // Parallel along the y axis, see if we're above or below of the rectangle.
                if (a.Y < top || a.Y > bottom)
                {
                    // Outside the box.
                    return false;
                }
            }
            else
            {
                var inverseDistanceY = 1f / dy;
// ReSharper disable RedundantCast Necessary for FarCollections.
                var t1 = (float) (top - a.Y) * inverseDistanceY;
                var t2 = (float) (bottom - a.Y) * inverseDistanceY;
// ReSharper restore RedundantCast

                if (t1 > t2)
                {
                    var tmp = t1;
                    t1 = t2;
                    t2 = tmp;
                }

                // Push the min up.
                if (t1 > tMin)
                {
                    tMin = t1;
                }

                // Pull the max down.
                if (t2 < tMax)
                {
                    tMax = t2;
                }

                if (tMin > tMax)
                {
                    return false;
                }
            }

            // Set out value to intersection value.
            tHit = tMin;

            // See if we hit something in the allowed interval.
            return tMin >= 0.0f && tMin < t;
#endif
        }
    }
}