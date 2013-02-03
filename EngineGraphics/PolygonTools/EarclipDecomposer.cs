/*
 * C# Version Ported by Matt Bettcher and Ian Qvist 2009-2010
 * 
 * Original C++ Version Copyright (c) 2007 Eric Jordan
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Engine.Graphics.PolygonTools
{
    /// <summary>
    ///     Ported from jBox2D. Original author: ewjordan
    ///     <para/>
    ///     Triangulates a polygon using simple ear-clipping algorithm.
    ///     <para/>
    ///     Only works on simple polygons.
    ///     <para/>
    ///     Triangles may be degenerate, especially if you have identical points in the input to the algorithm.  Check this
    ///     before you use them.
    /// </summary>
    public static class EarClipDecomposer
    {
        /// <summary>
        ///     box2D rev 32 - for details, see http://www.box2d.org/forum/viewtopic.php?f=4&t=83&start=50 and
        ///     http://www.ewjordan.com/earClip/
        /// </summary>
        private const float Tolerance = .001f;

        /// <summary>
        ///     Decomposes a non-convex polygon into a number of convex polygons, up to maxPolygons (remaining pieces are thrown
        ///     out). Each resulting polygon will have no more than Settings.MaxPolygonVertices vertices.
        ///     <para/>
        ///     Warning: Only works on simple polygons
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="maxPolygons">The maximum number of polygons.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns></returns>
        public static List<List<Vector2>> ConvexPartition(
            List<Vector2> vertices, int maxPolygons = int.MaxValue, float tolerance = 0)
        {
            if (vertices.Count < 3)
            {
                return new List<List<Vector2>> {vertices};
            }

            List<Triangle> triangulated;

            if (IsCounterClockWise(vertices))
            {
                var tempP = new List<Vector2>(vertices);
                tempP.Reverse();
                triangulated = TriangulatePolygon(tempP);
            }
            else
            {
                triangulated = TriangulatePolygon(vertices);
            }
            if (triangulated.Count < 1)
            {
                // Still no luck? Oh well...
                throw new Exception("Can't triangulate your polygon.");
            }

            var polygonizedTriangles = PolygonizeTriangles(triangulated, maxPolygons, tolerance);

            //The polygonized triangles are not guaranteed to be without collinear points. We remove
            //them to be sure.
            for (var i = 0; i < polygonizedTriangles.Count; i++)
            {
                polygonizedTriangles[i] = Simplification.CollinearSimplify(polygonizedTriangles[i], 0);
            }

            // Remove empty vertex collections.
            for (var i = polygonizedTriangles.Count - 1; i >= 0; i--)
            {
                if (polygonizedTriangles[i].Count == 0)
                {
                    polygonizedTriangles.RemoveAt(i);
                }
            }

            return polygonizedTriangles;
        }

        /// <summary>
        ///     Turns a list of triangles into a list of convex polygons. Very simple method - start with a seed triangle, keep
        ///     adding triangles to it until you can't add any more without making the polygon non-convex.
        ///     <para/>
        ///     Returns an integer telling how many polygons were created.  Will fill polygons array up to maxPolygons entries,
        ///     which may be smaller or larger than the return value.
        ///     <para/>
        ///     Takes O(N///P) where P is the number of resultant polygons, N is triangle count.
        ///     <para/>
        ///     The final polygon list will not necessarily be minimal, though in practice it works fairly well.
        /// </summary>
        /// <param name="triangulated">The triangulated.</param>
        /// <param name="maxPolygons">The maximum number of polygons</param>
        /// <param name="tolerance">The tolerance</param>
        /// <returns></returns>
        private static List<List<Vector2>> PolygonizeTriangles(
            IList<Triangle> triangulated, int maxPolygons, float tolerance)
        {
            var polygons = new List<List<Vector2>>(50);

            var polyIndex = 0;

            if (triangulated.Count <= 0)
            {
                // Return empty polygon list.
                return polygons;
            }

            var covered = new bool[triangulated.Count];
            for (var i = 0; i < triangulated.Count; ++i)
            {
                covered[i] = false;

                //Check here for degenerate triangles
// ReSharper disable CompareOfFloatsByEqualityOperator
                if (((triangulated[i].X[0] == triangulated[i].X[1]) && (triangulated[i].Y[0] == triangulated[i].Y[1]))
                    ||
                    ((triangulated[i].X[1] == triangulated[i].X[2]) && (triangulated[i].Y[1] == triangulated[i].Y[2]))
                    ||
                    ((triangulated[i].X[0] == triangulated[i].X[2]) && (triangulated[i].Y[0] == triangulated[i].Y[2])))
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    covered[i] = true;
                }
            }

            var notDone = true;
            while (notDone)
            {
                var current = -1;
                for (var i = 0; i < triangulated.Count; ++i)
                {
                    if (covered[i])
                    {
                        continue;
                    }
                    current = i;
                    break;
                }
                if (current == -1)
                {
                    notDone = false;
                }
                else
                {
                    var poly = new List<Vector2>(3);

                    for (var i = 0; i < 3; i++)
                    {
                        poly.Add(new Vector2(triangulated[current].X[i], triangulated[current].Y[i]));
                    }

                    covered[current] = true;
                    var index = 0;
                    for (var i = 0; i < 2 * triangulated.Count; ++i, ++index)
                    {
                        while (index >= triangulated.Count)
                        {
                            index -= triangulated.Count;
                        }
                        if (covered[index])
                        {
                            continue;
                        }
                        var newPolygon = AddTriangle(triangulated[index], poly);
                        if (newPolygon == null)
                        {
                            continue;
                        }

                        if (newPolygon.Count > 8)
                        {
                            continue;
                        }

                        if (IsConvex(newPolygon))
                        {
                            poly = new List<Vector2>(newPolygon);
                            covered[index] = true;
                        }
                    }

                    // We have a maximum of polygons that we need to keep under.
                    if (polyIndex < maxPolygons)
                    {
                        Simplification.MergeParallelEdges(poly, tolerance);

                        // If identical points are present, a triangle gets
                        // borked by the MergeParallelEdges function, hence
                        // the vertex number check.
                        if (poly.Count >= 3)
                        {
                            polygons.Add(new List<Vector2>(poly));
                        }
                    }

                    if (poly.Count >= 3)
                    {
                        polyIndex++;
                    }
                }
            }

            return polygons;
        }

        /// <summary>
        ///     Triangulates a polygon using simple ear-clipping algorithm. Returns size of Triangle array unless the polygon can't
        ///     be triangulated. This should only happen if the polygon self-intersects, though it will not _always_ return null
        ///     for a bad polygon - it is the caller's responsibility to check for self-intersection, and if it doesn't, it should
        ///     at least check that the return value is non-null before using. You're warned!
        ///     <para/>
        ///     Triangles may be degenerate, especially if you have identical points in the input to the algorithm.  Check this
        ///     before you use them.
        ///     <para/>
        ///     This is totally unoptimized, so for large polygons it should not be part of the simulation loop.
        ///     <para/>
        ///     Warning: Only works on simple polygons.
        /// </summary>
        private static List<Triangle> TriangulatePolygon(IList<Vector2> vertices)
        {
            var results = new List<Triangle>();
            if (vertices.Count < 3)
            {
                return new List<Triangle>();
            }

            //Recurse and split on pinch points
            List<Vector2> pA, pB;
            var pin = new List<Vector2>(vertices);
            if (ResolvePinchPoint(pin, out pA, out pB))
            {
                var mergeA = TriangulatePolygon(pA);
                var mergeB = TriangulatePolygon(pB);

                if (mergeA.Count == -1 || mergeB.Count == -1)
                {
                    throw new Exception("Can't triangulate your polygon.");
                }

                results.AddRange(mergeA.Select(t => new Triangle(t)));
                results.AddRange(mergeB.Select(t => new Triangle(t)));

                return results;
            }

            var buffer = new Triangle[vertices.Count - 2];
            var bufferSize = 0;
            var xrem = new float[vertices.Count];
            var yrem = new float[vertices.Count];
            for (var i = 0; i < vertices.Count; ++i)
            {
                xrem[i] = vertices[i].X;
                yrem[i] = vertices[i].Y;
            }

            var count = vertices.Count;

            while (count > 3)
            {
                // Find an ear
                var earIndex = -1;
                var earMaxMinCross = -10.0f;
                for (var i = 0; i < count; ++i)
                {
                    if (IsEar(i, xrem, yrem, count))
                    {
                        var lower = Remainder(i - 1, count);
                        var upper = Remainder(i + 1, count);
                        var d1 = new Vector2(xrem[upper] - xrem[i], yrem[upper] - yrem[i]);
                        var d2 = new Vector2(xrem[i] - xrem[lower], yrem[i] - yrem[lower]);
                        var d3 = new Vector2(xrem[lower] - xrem[upper], yrem[lower] - yrem[upper]);

                        d1.Normalize();
                        d2.Normalize();
                        d3.Normalize();

                        var cross12 = System.Math.Abs(d1.X * d2.Y - d1.Y * d2.X);
                        var cross23 = System.Math.Abs(d2.X * d3.Y - d2.Y * d3.X);
                        var cross31 = System.Math.Abs(d3.X * d1.Y - d3.Y * d1.X);

                        //Find the maximum minimum angle
                        var minCross = System.Math.Min(cross12, System.Math.Min(cross23, cross31));
                        if (minCross > earMaxMinCross)
                        {
                            earIndex = i;
                            earMaxMinCross = minCross;
                        }
                    }
                }

                // If we still haven't found an ear, we're screwed.
                // Note: sometimes this is happening because the
                // remaining points are collinear.  Really these
                // should just be thrown out without halting triangulation.
                if (earIndex == -1)
                {
                    for (var i = 0; i < bufferSize; i++)
                    {
                        results.Add(new Triangle(buffer[i]));
                    }

                    return results;
                }

                // Clip off the ear:
                // - remove the ear tip from the list
                --count;
                var newx = new float[count];
                var newy = new float[count];
                var current = 0;
                for (var i = 0; i < count; ++i)
                {
                    if (current == earIndex)
                    {
                        ++current;
                    }
                    newx[i] = xrem[current];
                    newy[i] = yrem[current];
                    ++current;
                }

                // - add the clipped triangle to the triangle list
                var under = (earIndex == 0) ? (count) : (earIndex - 1);
                var over = (earIndex == count) ? 0 : (earIndex + 1);
                buffer[bufferSize] = new Triangle(
                    xrem[earIndex], yrem[earIndex], xrem[over], yrem[over], xrem[under], yrem[under]);
                ++bufferSize;

                // - replace the old list with the new one
                xrem = newx;
                yrem = newy;
            }

            buffer[bufferSize] = new Triangle(xrem[1], yrem[1], xrem[2], yrem[2], xrem[0], yrem[0]);
            ++bufferSize;

            for (var i = 0; i < bufferSize; i++)
            {
                results.Add(new Triangle(buffer[i]));
            }

            return results;
        }

        /// <summary>
        ///     Finds and fixes "pinch points," points where two polygon vertices are at the same point.
        ///     <para/>
        ///     If a pinch point is found, pin is broken up into poutA and poutB and true is returned; otherwise, returns false.
        ///     <para/>
        ///     Mostly for internal use.
        ///     <para/>
        ///     O(N^2) time, which sucks...
        /// </summary>
        private static bool ResolvePinchPoint(IList<Vector2> pin, out List<Vector2> poutA, out List<Vector2> poutB)
        {
            poutA = new List<Vector2>();
            poutB = new List<Vector2>();

            if (pin.Count < 3)
            {
                return false;
            }

            var hasPinchPoint = false;
            var pinchIndexA = -1;
            var pinchIndexB = -1;
            for (var i = 0; i < pin.Count; ++i)
            {
                for (var j = i + 1; j < pin.Count; ++j)
                {
                    // Don't worry about pinch points where the points
                    // are actually just dupe neighbors.
                    if (System.Math.Abs(pin[i].X - pin[j].X) < Tolerance &&
                        System.Math.Abs(pin[i].Y - pin[j].Y) < Tolerance &&
                        j != i + 1)
                    {
                        pinchIndexA = i;
                        pinchIndexB = j;
                        hasPinchPoint = true;
                        break;
                    }
                }
                if (hasPinchPoint)
                {
                    break;
                }
            }
            if (hasPinchPoint)
            {
                var sizeA = pinchIndexB - pinchIndexA;
                if (sizeA == pin.Count)
                {
                    return false; // Has dupe points at wraparound, not a problem here.
                }
                for (var i = 0; i < sizeA; ++i)
                {
                    poutA.Add(pin[Remainder(pinchIndexA + i, pin.Count)]);
                }

                var sizeB = pin.Count - sizeA;
                for (var i = 0; i < sizeB; ++i)
                {
                    poutB.Add(pin[Remainder(pinchIndexB + i, pin.Count)]);
                }
            }
            return hasPinchPoint;
        }

        /// <summary>Fix for obnoxious behavior for the % operator for negative numbers...</summary>
        private static int Remainder(int x, int modulus)
        {
            var rem = x % modulus;
            while (rem < 0)
            {
                rem += modulus;
            }
            return rem;
        }

        private static List<Vector2> AddTriangle(Triangle t, IList<Vector2> vertices)
        {
            // First, find vertices that connect.
            var firstP = -1;
            var firstT = -1;
            var secondP = -1;
            var secondT = -1;
            for (var i = 0; i < vertices.Count; i++)
            {
// ReSharper disable CompareOfFloatsByEqualityOperator
                if (t.X[0] == vertices[i].X && t.Y[0] == vertices[i].Y)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    if (firstP == -1)
                    {
                        firstP = i;
                        firstT = 0;
                    }
                    else
                    {
                        secondP = i;
                        secondT = 0;
                    }
                }
// ReSharper disable CompareOfFloatsByEqualityOperator
                else if (t.X[1] == vertices[i].X && t.Y[1] == vertices[i].Y)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    if (firstP == -1)
                    {
                        firstP = i;
                        firstT = 1;
                    }
                    else
                    {
                        secondP = i;
                        secondT = 1;
                    }
                }
// ReSharper disable CompareOfFloatsByEqualityOperator
                else if (t.X[2] == vertices[i].X && t.Y[2] == vertices[i].Y)
// ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    if (firstP == -1)
                    {
                        firstP = i;
                        firstT = 2;
                    }
                    else
                    {
                        secondP = i;
                        secondT = 2;
                    }
                }
            }
            // Fix ordering if first should be last vertex of poly
            if (firstP == 0 && secondP == vertices.Count - 1)
            {
                firstP = vertices.Count - 1;
                secondP = 0;
            }

            // Didn't find it
            if (secondP == -1)
            {
                return null;
            }

            // Find tip index on triangle
            var tipT = 0;
            if (tipT == firstT || tipT == secondT)
            {
                tipT = 1;
            }
            if (tipT == firstT || tipT == secondT)
            {
                tipT = 2;
            }

            var result = new List<Vector2>(vertices.Count + 1);
            for (int i = 0; i < vertices.Count; i++)
            {
                result.Add(vertices[i]);

                if (i == firstP)
                {
                    result.Add(new Vector2(t.X[tipT], t.Y[tipT]));
                }
            }

            return result;
        }

        /// <summary>
        ///     Checks if vertex i is the tip of an ear in polygon defined by xv[] and yv[].
        ///     <para/>
        ///     Assumes clockwise orientation of polygon...
        /// </summary>
        private static bool IsEar(int i, float[] xv, float[] yv, int xvLength)
        {
            float dx0, dy0, dx1, dy1;
            if (i >= xvLength || i < 0 || xvLength < 3)
            {
                return false;
            }
            var upper = i + 1;
            var lower = i - 1;
            if (i == 0)
            {
                dx0 = xv[0] - xv[xvLength - 1];
                dy0 = yv[0] - yv[xvLength - 1];
                dx1 = xv[1] - xv[0];
                dy1 = yv[1] - yv[0];
                lower = xvLength - 1;
            }
            else if (i == xvLength - 1)
            {
                dx0 = xv[i] - xv[i - 1];
                dy0 = yv[i] - yv[i - 1];
                dx1 = xv[0] - xv[i];
                dy1 = yv[0] - yv[i];
                upper = 0;
            }
            else
            {
                dx0 = xv[i] - xv[i - 1];
                dy0 = yv[i] - yv[i - 1];
                dx1 = xv[i + 1] - xv[i];
                dy1 = yv[i + 1] - yv[i];
            }
            var cross = dx0 * dy1 - dx1 * dy0;
            if (cross > 0)
            {
                return false;
            }
            var triangle = new Triangle(
                xv[i],
                yv[i],
                xv[upper],
                yv[upper],
                xv[lower],
                yv[lower]);
            for (var j = 0; j < xvLength; ++j)
            {
                if (j == i || j == lower || j == upper)
                {
                    continue;
                }
                if (triangle.IsInside(xv[j], yv[j]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Assuming the polygon is simple; determines whether the polygon is convex.
        ///     <para/>
        ///     NOTE: It will also return false if the input contains collinear edges.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if it is convex; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsConvex(IList<Vector2> polygon)
        {
            // Ensure the polygon is convex and the interior
            // is to the left of each edge.
            for (var i = 0; i < polygon.Count; ++i)
            {
                var i1 = i;
                var i2 = i + 1 < polygon.Count ? i + 1 : 0;
                var edge = polygon[i2] - polygon[i1];

                for (var j = 0; j < polygon.Count; ++j)
                {
                    // Don't check vertices on the current edge.
                    if (j == i1 || j == i2)
                    {
                        continue;
                    }

                    var r = polygon[j] - polygon[i1];

                    if (edge.X * r.Y - edge.Y * r.X <= 0.0f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool IsCounterClockWise(IList<Vector2> polygon)
        {
            // We just return true for lines.
            if (polygon.Count < 3)
            {
                return true;
            }

            var area = 0f;
            for (var i = 0; i < polygon.Count; i++)
            {
                var j = (i + 1) % polygon.Count;
                area += polygon[i].X * polygon[j].Y;
                area -= polygon[i].Y * polygon[j].X;
            }
            return area / 2f > 0f;
        }

        private class Triangle
        {
            public readonly float[] X;
            public readonly float[] Y;

            /// <summary>Constructor automatically fixes orientation to counter clockwise.</summary>
            public Triangle(float x1, float y1, float x2, float y2, float x3, float y3)
            {
                X = new float[3];
                Y = new float[3];
                var dx1 = x2 - x1;
                var dx2 = x3 - x1;
                var dy1 = y2 - y1;
                var dy2 = y3 - y1;
                var cross = dx1 * dy2 - dx2 * dy1;
                var counterClockwise = (cross > 0);
                if (counterClockwise)
                {
                    X[0] = x1;
                    X[1] = x2;
                    X[2] = x3;
                    Y[0] = y1;
                    Y[1] = y2;
                    Y[2] = y3;
                }
                else
                {
                    X[0] = x1;
                    X[1] = x3;
                    X[2] = x2;
                    Y[0] = y1;
                    Y[1] = y3;
                    Y[2] = y2;
                }
            }

            public Triangle(Triangle t)
            {
                X = new float[3];
                Y = new float[3];

                X[0] = t.X[0];
                X[1] = t.X[1];
                X[2] = t.X[2];
                Y[0] = t.Y[0];
                Y[1] = t.Y[1];
                Y[2] = t.Y[2];
            }

            public bool IsInside(float x, float y)
            {
                if (x < X[0] && x < X[1] && x < X[2])
                {
                    return false;
                }
                if (x > X[0] && x > X[1] && x > X[2])
                {
                    return false;
                }
                if (y < Y[0] && y < Y[1] && y < Y[2])
                {
                    return false;
                }
                if (y > Y[0] && y > Y[1] && y > Y[2])
                {
                    return false;
                }

                var vx2 = x - X[0];
                var vy2 = y - Y[0];
                var vx1 = X[1] - X[0];
                var vy1 = Y[1] - Y[0];
                var vx0 = X[2] - X[0];
                var vy0 = Y[2] - Y[0];

                var dot00 = vx0 * vx0 + vy0 * vy0;
                var dot01 = vx0 * vx1 + vy0 * vy1;
                var dot02 = vx0 * vx2 + vy0 * vy2;
                var dot11 = vx1 * vx1 + vy1 * vy1;
                var dot12 = vx1 * vx2 + vy1 * vy2;
                var invDenom = 1.0f / (dot00 * dot11 - dot01 * dot01);
                var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
                var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

                return ((u > 0) && (v > 0) && (u + v < 1));
            }
        }
    }
}