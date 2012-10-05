using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.Common
{
#if !(XBOX360)
    [DebuggerDisplay("Count = {Count} Vertices = {ToString()}")]
#endif
    public class Vertices : List<Vector2>
    {
        public Vertices()
        {
        }

        public Vertices(int capacity)
        {
            Capacity = capacity;
        }

        public Vertices(Vector2[] vector2)
        {
            for (int i = 0; i < vector2.Length; i++)
            {
                Add(vector2[i]);
            }
        }

        public Vertices(IList<Vector2> vertices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Add(vertices[i]);
            }
        }

        /// <summary>
        /// Nexts the index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public int NextIndex(int index)
        {
            if (index == Count - 1)
            {
                return 0;
            }
            return index + 1;
        }

        /// <summary>
        /// Gets the previous index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public int PreviousIndex(int index)
        {
            if (index == 0)
            {
                return Count - 1;
            }
            return index - 1;
        }

        /// <summary>
        /// Gets the signed area.
        /// </summary>
        /// <returns></returns>
        public float GetSignedArea()
        {
            int i;
            float area = 0;

            for (i = 0; i < Count; i++)
            {
                int j = (i + 1) % Count;
                area += this[i].X * this[j].Y;
                area -= this[i].Y * this[j].X;
            }
            area /= 2.0f;
            return area;
        }

        /// <summary>
        /// Gets the area.
        /// </summary>
        /// <returns></returns>
        public float GetArea()
        {
            int i;
            float area = 0;

            for (i = 0; i < Count; i++)
            {
                int j = (i + 1) % Count;
                area += this[i].X * this[j].Y;
                area -= this[i].Y * this[j].X;
            }
            area /= 2.0f;
            return (area < 0 ? -area : area);
        }

        /// <summary>
        /// Gets the centroid.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetCentroid()
        {
            // Same algorithm is used by Box2D

            Vector2 c = Vector2.Zero;
            float area = 0.0f;

            const float inv3 = 1.0f / 3.0f;
            Vector2 pRef = Vector2.Zero;
            for (int i = 0; i < Count; ++i)
            {
                // Triangle vertices.
                Vector2 p1 = pRef;
                Vector2 p2 = this[i];
                Vector2 p3 = i + 1 < Count ? this[i + 1] : this[0];

                Vector2 e1 = p2 - p1;
                Vector2 e2 = p3 - p1;

                float D = MathUtils.Cross(e1, e2);

                float triangleArea = 0.5f * D;
                area += triangleArea;

                // Area weighted centroid
                c += triangleArea * inv3 * (p1 + p2 + p3);
            }

            // Centroid
            c *= 1.0f / area;
            return c;
        }

        /// <summary>
        /// Returns an AABB for vertex.
        /// </summary>
        /// <returns></returns>
        public void GetCollisionBox(out Vector2 lowerBound, out Vector2 upperBound)
        {
            lowerBound = new Vector2(float.MaxValue, float.MaxValue);
            upperBound = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < Count; ++i)
            {
                if (this[i].X < lowerBound.X)
                {
                    lowerBound.X = this[i].X;
                }
                if (this[i].X > upperBound.X)
                {
                    upperBound.X = this[i].X;
                }

                if (this[i].Y < lowerBound.Y)
                {
                    lowerBound.Y = this[i].Y;
                }
                if (this[i].Y > upperBound.Y)
                {
                    upperBound.Y = this[i].Y;
                }
            }
        }

        public void Translate(Vector2 vector)
        {
            Translate(ref vector);
        }

        /// <summary>
        /// Translates the vertices with the specified vector.
        /// </summary>
        /// <param name="vector">The vector.</param>
        public void Translate(ref Vector2 vector)
        {
            for (int i = 0; i < Count; i++)
                this[i] = Vector2.Add(this[i], vector);
        }

        /// <summary>
        /// Scales the vertices with the specified vector.
        /// </summary>
        /// <param name="value">The Value.</param>
        public void Scale(ref Vector2 value)
        {
            for (int i = 0; i < Count; i++)
                this[i] = Vector2.Multiply(this[i], value);
        }

        /// <summary>
        /// Rotate the vertices with the defined value in radians.
        /// </summary>
        /// <param name="value">The amount to rotate by in radians.</param>
        public void Rotate(float value)
        {
            Matrix rotationMatrix;
            Matrix.CreateRotationZ(value, out rotationMatrix);

            for (int i = 0; i < Count; i++)
                this[i] = Vector2.Transform(this[i], rotationMatrix);
        }

        /// <summary>
        /// Assuming the polygon is simple; determines whether the polygon is convex.
        /// NOTE: It will also return false if the input contains colinear edges.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if it is convex; otherwise, <c>false</c>.
        /// </returns>
        public bool IsConvex()
        {
            // Ensure the polygon is convex and the interior
            // is to the left of each edge.
            for (int i = 0; i < Count; ++i)
            {
                int i1 = i;
                int i2 = i + 1 < Count ? i + 1 : 0;
                Vector2 edge = this[i2] - this[i1];

                for (int j = 0; j < Count; ++j)
                {
                    // Don't check vertices on the current edge.
                    if (j == i1 || j == i2)
                    {
                        continue;
                    }

                    Vector2 r = this[j] - this[i1];

                    float s = edge.X * r.Y - edge.Y * r.X;

                    if (s <= 0.0f)
                        return false;
                }
            }
            return true;
        }

        public bool IsCounterClockWise()
        {
            //We just return true for lines
            if (Count < 3)
                return true;

            return (GetSignedArea() > 0.0f);
        }

        /// <summary>
        /// Forces counter clock wise order.
        /// </summary>
        public void ForceCounterClockWise()
        {
            if (!IsCounterClockWise())
            {
                Reverse();
            }
        }

        /// <summary>
        /// Check for edge crossings
        /// </summary>
        /// <returns></returns>
        public bool IsSimple()
        {
            for (var i = 0; i < Count; ++i)
            {
                var iplus = (i + 1 > Count - 1) ? 0 : i + 1;
                var a1 = new Vector2(this[i].X, this[i].Y);
                var a2 = new Vector2(this[iplus].X, this[iplus].Y);
                for (var j = i + 1; j < Count; ++j)
                {
                    var next = (j + 1 > Count - 1) ? 0 : j + 1;
                    var b1 = new Vector2(this[j].X, this[j].Y);
                    var b2 = new Vector2(this[next].X, this[next].Y);

                    Vector2 dummy;
                    if (LineTools.LineIntersect2(a1, a2, b1, b2, out dummy))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < Count; i++)
            {
                builder.Append(this[i]);
                if (i < Count - 1)
                {
                    builder.Append(" ");
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Winding number test for a point in a polygon.
        /// </summary>
        /// See more info about the algorithm here: http://softsurfer.com/Archive/algorithm_0103/algorithm_0103.htm
        /// <param name="point">The point to be tested.</param>
        /// <returns>-1 if the winding number is zero and the point is outside
        /// the polygon, 1 if the point is inside the polygon, and 0 if the point
        /// is on the polygons edge.</returns>
        public int PointInPolygon(ref Vector2 point)
        {
            // Winding number
            int wn = 0;

            // Iterate through polygon's edges
            for (int i = 0; i < Count; i++)
            {
                // Get points
                Vector2 p1 = this[i];
                Vector2 p2 = this[NextIndex(i)];

                // Test if a point is directly on the edge
                Vector2 edge = p2 - p1;
                float area = MathUtils.Area(ref p1, ref p2, ref point);
                if (area == 0f && Vector2.Dot(point - p1, edge) >= 0f && Vector2.Dot(point - p2, edge) <= 0f)
                {
                    return 0;
                }
                // Test edge for intersection with ray from point
                if (p1.Y <= point.Y)
                {
                    if (p2.Y > point.Y && area > 0f)
                    {
                        ++wn;
                    }
                }
                else
                {
                    if (p2.Y <= point.Y && area < 0f)
                    {
                        --wn;
                    }
                }
            }
            return (wn == 0 ? -1 : 1);
        }
    }
}