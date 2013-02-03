using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;

namespace Engine.Graphics.PolygonTools
{
    /// <summary>
    ///     Implements "A new algorithm for Boolean operations on general polygons" available here:
    ///     http://liama.ia.ac.cn/wiki/_media/user:dong:dong_cg_05.pdf
    ///     <para/>
    ///     Merges two polygons, a subject and a clip with the specified operation. Polygons may not be self-intersecting.
    ///     <para/>
    ///     Warning: May yield incorrect results or even crash if polygons contain collinear points.
    ///     <para/>
    ///     Originally from FarSeer Physics, contributed by Helge Backhaus.
    /// </summary>
    public static class BooleanOperations
    {
        #region Public methods

        /// <summary>Combines two polygons to form a polygon covering the area of the two input polygons.</summary>
        /// <param name="polygon1">The first polygon.</param>
        /// <param name="polygon2">The second polygon.</param>
        /// <param name="error">The result error.</param>
        /// <returns>The resulting polygon.</returns>
        [PublicAPI]
        public static List<List<Vector2>> Union(List<Vector2> polygon1, List<Vector2> polygon2, out PolyClipError error)
        {
            return Execute(polygon1, polygon2, PolyClipType.Union, out error);
        }

        /// <summary>Combines two polygons to form a polygon covering the difference area of the two input polygons.</summary>
        /// <param name="polygon1">The first polygon.</param>
        /// <param name="polygon2">The second polygon.</param>
        /// <param name="error">The result error.</param>
        /// <returns>The resulting polygon.</returns>
        [PublicAPI]
        public static List<List<Vector2>> Difference(
            List<Vector2> polygon1, List<Vector2> polygon2, out PolyClipError error)
        {
            return Execute(polygon1, polygon2, PolyClipType.Difference, out error);
        }

        /// <summary>Combines two polygons to form a polygon covering the shared area of the two input polygons.</summary>
        /// <param name="polygon1">The first polygon.</param>
        /// <param name="polygon2">The second polygon.</param>
        /// <param name="error">The result error.</param>
        /// <returns>The resulting polygon.</returns>
        [PublicAPI]
        public static List<List<Vector2>> Intersect(
            List<Vector2> polygon1, List<Vector2> polygon2, out PolyClipError error)
        {
            return Execute(polygon1, polygon2, PolyClipType.Intersect, out error);
        }

        #endregion

        private const float ClipperEpsilonSquared = 1.192092896e-07f;

        private enum PolyClipType
        {
            Intersect,
            Union,
            Difference
        }

        public enum PolyClipError
        {
            None,
            DegeneratedOutput,
            BrokenResult
        }

        /// <summary>Actual algorithm.</summary>
        /// <param name="subject">The subject polygon.</param>
        /// <param name="clip">The clip polygon, which is added, subtracted or intersected with the subject</param>
        /// <param name="clipType">The operation to be performed. Either Union, Difference or Intersection.</param>
        /// <param name="error">The error generated (if any)</param>
        /// <returns>
        ///     A list of closed polygons, which make up the result of the clipping operation. Outer contours are ordered
        ///     counter clockwise, holes are ordered clockwise.
        /// </returns>
        private static List<List<Vector2>> Execute(
            IList<Vector2> subject, IList<Vector2> clip, PolyClipType clipType, out PolyClipError error)
        {
            if (!IsSimple(subject))
            {
                throw new ArgumentException(
                    "Input subject polygon must be simple (cannot intersect themselves).", "subject");
            }
            if (!IsSimple(clip))
            {
                throw new ArgumentException("Input clip polygon must be simple (cannot intersect themselves).", "clip");
            }

            // Copy polygons.
            List<Vector2> slicedSubject;
            List<Vector2> slicedClip;

            // Calculate the intersection and touch points between subject and clip and add them to both.
            CalculateIntersections(subject, clip, out slicedSubject, out slicedClip);

            // Translate polygons into upper right quadrant as the algorithm depends on it.
            var lbSubject = GetLowerBound(subject);
            var lbClip = GetLowerBound(clip);
            Vector2 translate;
            Vector2.Min(ref lbSubject, ref lbClip, out translate);
            translate = Vector2.One - translate;
            if (translate != Vector2.Zero)
            {
                for (int i = 0, count = slicedSubject.Count; i < count; ++i)
                {
                    slicedSubject[i] += translate;
                }
                for (int i = 0, count = slicedClip.Count; i < count; ++i)
                {
                    slicedClip[i] += translate;
                }
            }

            // Enforce counterclockwise contours.
            ForceCounterClockWise(slicedSubject);
            ForceCounterClockWise(slicedClip);

            // Build simplical chains from the polygons and calculate the the corresponding coefficients.
            List<Edge> subjectSimplices;
            List<float> subjectCoefficient;
            List<Edge> clipSimplices;
            List<float> clipCoefficient;
            CalculateSimplicalChain(slicedSubject, out subjectCoefficient, out subjectSimplices);
            CalculateSimplicalChain(slicedClip, out clipCoefficient, out clipSimplices);

            // Determine the characteristics function for all non-original edges
            // in subject and clip simplical chain and combine the edges contributing
            // to the result, depending on the clipType
            var resultSimplices = CalculateResultChain(
                subjectCoefficient,
                subjectSimplices,
                clipCoefficient,
                clipSimplices,
                clipType);

            // Convert result chain back to polygon(s).
            List<List<Vector2>> result;
            error = BuildPolygonsFromChain(resultSimplices, out result);

            // Reverse the polygon translation from the beginning
            // and remove collinear points from output
            translate *= -1.0f;
            foreach (var vertices in result)
            {
                for (int i = 0, count = vertices.Count; i < count; ++i)
                {
                    vertices[i] += translate;
                }
                Simplification.CollinearSimplify(vertices);
            }
            return result;
        }

        /// <summary>Checks if the vertices form an simple polygon by checking for edge crossings.</summary>
        private static bool IsSimple(IList<Vector2> vertices)
        {
            for (int i = 0, count = vertices.Count; i < count; ++i)
            {
                var a1 = vertices[i];
                var a2 = vertices[(i + 1) % count];
                for (var j = i + 1; j < count; ++j)
                {
                    var b1 = vertices[j];
                    var b2 = vertices[(j + 1) % count];

                    Vector2 temp;
                    if (LineIntersect(a1, a2, b1, b2, out temp))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // From Eric Jordan's convex decomposition library
        /// <summary>
        ///     Check if the lines a0->a1 and b0->b1 cross. If they do, intersectionPoint will be filled with the point of
        ///     crossing. Grazing lines should not return true.
        /// </summary>
        /// <param name="a0"></param>
        /// <param name="a1"></param>
        /// <param name="b0"></param>
        /// <param name="b1"></param>
        /// <param name="intersectionPoint"></param>
        /// <returns></returns>
        private static bool LineIntersect(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, out Vector2 intersectionPoint)
        {
            intersectionPoint = Vector2.Zero;

            if (a0 == b0 || a0 == b1 || a1 == b0 || a1 == b1)
            {
                return false;
            }

            // AABB early exit.
            if (System.Math.Max(a0.X, a1.X) < System.Math.Min(b0.X, b1.X) ||
                System.Math.Max(b0.X, b1.X) < System.Math.Min(a0.X, a1.X))
            {
                return false;
            }

            if (System.Math.Max(a0.Y, a1.Y) < System.Math.Min(b0.Y, b1.Y) ||
                System.Math.Max(b0.Y, b1.Y) < System.Math.Min(a0.Y, a1.Y))
            {
                return false;
            }

            var ua = (b1.X - b0.X) * (a0.Y - b0.Y) - (b1.Y - b0.Y) * (a0.X - b0.X);
            var ub = (a1.X - a0.X) * (a0.Y - b0.Y) - (a1.Y - a0.Y) * (a0.X - b0.X);
            var denominator = (b1.Y - b0.Y) * (a1.X - a0.X) - (b1.X - b0.X) * (a1.Y - a0.Y);
            if (System.Math.Abs(denominator) < ClipperEpsilonSquared)
            {
                // Lines are too close to parallel to call.
                return false;
            }
            ua /= denominator;
            ub /= denominator;

            if ((0 < ua) && (ua < 1) && (0 < ub) && (ub < 1))
            {
                intersectionPoint.X = (a0.X + ua * (a1.X - a0.X));
                intersectionPoint.Y = (a0.Y + ua * (a1.Y - a0.Y));
                return true;
            }

            return false;
        }

        /// <summary>Computes the lower bounds of a polygon.</summary>
        /// <param name="vertices">The polygon.</param>
        /// <returns>The lower bound.</returns>
        private static Vector2 GetLowerBound(IList<Vector2> vertices)
        {
            var lowerBound = vertices[0];
            for (int i = 1, count = vertices.Count; i < count; ++i)
            {
                lowerBound = Vector2.Min(lowerBound, vertices[i]);
            }
            return lowerBound;
        }

        /// <summary>Forces the vertices to be counter clock wise order.</summary>
        private static void ForceCounterClockWise(List<Vector2> vertices)
        {
            var area = 0.0f;
            for (int i = 0, count = vertices.Count; i < count; i++)
            {
                var next = (i + 1) % count;

                var vi = vertices[i];
                var vj = vertices[next];

                area += vi.X * vj.Y;
                area -= vi.Y * vj.X;
            }

            if (area < 0.0f)
            {
                vertices.Reverse();
            }
        }

        /// <summary>Calculates all intersections between two polygons.</summary>
        /// <param name="polygon1">The first polygon.</param>
        /// <param name="polygon2">The second polygon.</param>
        /// <param name="slicedPoly1">Returns the first polygon with added intersection points.</param>
        /// <param name="slicedPoly2">Returns the second polygon with added intersection points.</param>
        private static void CalculateIntersections(
            IList<Vector2> polygon1,
            IList<Vector2> polygon2,
            out List<Vector2> slicedPoly1,
            out List<Vector2> slicedPoly2)
        {
            slicedPoly1 = new List<Vector2>(polygon1);
            slicedPoly2 = new List<Vector2>(polygon2);

            // Iterate through polygon1's edges
            for (var i = 0; i < polygon1.Count; i++)
            {
                // Get edge vertices
                var a = polygon1[i];
                var b = polygon1[(i + 1) % polygon1.Count];

                // Get intersections between this edge and polygon2
                for (var j = 0; j < polygon2.Count; j++)
                {
                    var c = polygon2[j];
                    var d = polygon2[(j + 1) % polygon2.Count];

                    Vector2 intersectionPoint;
                    // Check if the edges intersect
                    if (LineIntersect(a, b, c, d, out intersectionPoint))
                    {
                        // Calculate alpha values for sorting multiple intersections points on a edge.
                        // Insert intersection point into first polygon.
                        float alpha = GetAlpha(a, b, intersectionPoint);
                        if (alpha > 0f && alpha < 1f)
                        {
                            var index = slicedPoly1.IndexOf(a) + 1;
                            while (index < slicedPoly1.Count &&
                                   GetAlpha(a, b, slicedPoly1[index]) <= alpha)
                            {
                                ++index;
                            }
                            slicedPoly1.Insert(index, intersectionPoint);
                        }
                        // Insert intersection point into second polygon
                        alpha = GetAlpha(c, d, intersectionPoint);
                        if (alpha > 0f && alpha < 1f)
                        {
                            var index = slicedPoly2.IndexOf(c) + 1;
                            while (index < slicedPoly2.Count &&
                                   GetAlpha(c, d, slicedPoly2[index]) <= alpha)
                            {
                                ++index;
                            }
                            slicedPoly2.Insert(index, intersectionPoint);
                        }
                    }
                }
            }

            // Check for very small edges
            for (var i = slicedPoly1.Count - 1; i >= 0; --i)
            {
                if ((slicedPoly1[(i + 1) % slicedPoly1.Count] - slicedPoly1[i]).LengthSquared() <= ClipperEpsilonSquared)
                {
                    slicedPoly1.RemoveAt(i);
                }
            }
            for (var i = slicedPoly2.Count - 1; i >= 0; --i)
            {
                if ((slicedPoly2[(i + 1) % slicedPoly2.Count] - slicedPoly2[i]).LengthSquared() <= ClipperEpsilonSquared)
                {
                    slicedPoly2.RemoveAt(i);
                }
            }
        }

        /// <summary>Calculates the simplical chain corresponding to the input polygon.</summary>
        private static void CalculateSimplicalChain(
            IList<Vector2> poly, out List<float> coefficient, out List<Edge> simplices)
        {
            simplices = new List<Edge>();
            coefficient = new List<float>();
            for (var i = 0; i < poly.Count; ++i)
            {
                simplices.Add(new Edge(poly[i], poly[(i + 1) % poly.Count]));
                coefficient.Add(CalculateSimplexCoefficient(Vector2.Zero, poly[i], poly[(i + 1) % poly.Count]));
            }
        }

        /// <summary>
        ///     Calculates the characteristics function for all edges of the given simplical chains and builds the result
        ///     chain.
        /// </summary>
        private static List<Edge> CalculateResultChain(
            IList<float> poly1Coefficient,
            IList<Edge> poly1Simplices,
            IList<float> poly2Coefficient,
            IList<Edge> poly2Simplices,
            PolyClipType clipType)
        {
            var resultSimplices = new List<Edge>();

            foreach (var simplex in poly1Simplices)
            {
                var edgeCharacter = 0.0f;
                if (poly2Simplices.Contains(simplex) ||
                    (poly2Simplices.Contains(-simplex) && clipType == PolyClipType.Union))
                {
                    edgeCharacter = 1.0f;
                }
                else
                {
                    for (var j = 0; j < poly2Simplices.Count; ++j)
                    {
                        if (!poly2Simplices.Contains(-simplex))
                        {
                            edgeCharacter += CalculateBeta(
                                simplex.GetCenter(), poly2Simplices[j], poly2Coefficient[j]);
                        }
                    }
                }

                switch (clipType)
                {
                    case PolyClipType.Intersect:
// ReSharper disable CompareOfFloatsByEqualityOperator
                        if (edgeCharacter == 1.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
                        {
                            resultSimplices.Add(simplex);
                        }
                        break;
                    default:
// ReSharper disable CompareOfFloatsByEqualityOperator
                        if (edgeCharacter == 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
                        {
                            resultSimplices.Add(simplex);
                        }
                        break;
                }
            }

            foreach (var simplex in poly2Simplices)
            {
                if (resultSimplices.Contains(simplex) || resultSimplices.Contains(-simplex))
                {
                    continue;
                }

                var edgeCharacter = 0.0f;
                if (poly1Simplices.Contains(simplex) ||
                    (poly1Simplices.Contains(-simplex) && clipType == PolyClipType.Union))
                {
                    edgeCharacter = 1.0f;
                }
                else
                {
                    for (var j = 0; j < poly1Simplices.Count; ++j)
                    {
                        if (!poly1Simplices.Contains(-simplex))
                        {
                            edgeCharacter += CalculateBeta(
                                simplex.GetCenter(), poly1Simplices[j], poly1Coefficient[j]);
                        }
                    }
                }

                switch (clipType)
                {
                    case PolyClipType.Difference:
                    case PolyClipType.Intersect:
// ReSharper disable CompareOfFloatsByEqualityOperator
                        if (edgeCharacter == 1.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
                        {
                            resultSimplices.Add(-simplex);
                        }
                        break;
                    default:
// ReSharper disable CompareOfFloatsByEqualityOperator
                        if (edgeCharacter == 0.0f)
// ReSharper restore CompareOfFloatsByEqualityOperator
                        {
                            resultSimplices.Add(simplex);
                        }
                        break;
                }
            }

            return resultSimplices;
        }

        /// <summary>Calculates the polygon(s) from the result simplical chain.</summary>
        private static PolyClipError BuildPolygonsFromChain(IList<Edge> simplices, out List<List<Vector2>> result)
        {
            result = new List<List<Vector2>>();
            var errVal = PolyClipError.None;

            while (simplices.Count > 0)
            {
                var output = new List<Vector2> {simplices[0].EdgeStart, simplices[0].EdgeEnd};
                simplices.RemoveAt(0);
                var closed = false;
                var index = 0;
                var count = simplices.Count; // Needed to catch infinite loops
                while (!closed && simplices.Count > 0)
                {
                    if (VectorEqual(output[output.Count - 1], simplices[index].EdgeStart))
                    {
                        if (VectorEqual(simplices[index].EdgeEnd, output[0]))
                        {
                            closed = true;
                        }
                        else
                        {
                            output.Add(simplices[index].EdgeEnd);
                        }
                        simplices.RemoveAt(index);
                        --index;
                    }
                    else if (VectorEqual(output[output.Count - 1], simplices[index].EdgeEnd))
                    {
                        if (VectorEqual(simplices[index].EdgeStart, output[0]))
                        {
                            closed = true;
                        }
                        else
                        {
                            output.Add(simplices[index].EdgeStart);
                        }
                        simplices.RemoveAt(index);
                        --index;
                    }
                    if (!closed)
                    {
                        if (++index == simplices.Count)
                        {
                            if (count == simplices.Count)
                            {
                                result = new List<List<Vector2>>();
                                Debug.WriteLine("Undefined error while building result polygon(s).");
                                return PolyClipError.BrokenResult;
                            }
                            index = 0;
                            count = simplices.Count;
                        }
                    }
                }
                if (output.Count < 3)
                {
                    errVal = PolyClipError.DegeneratedOutput;
                    Debug.WriteLine("Degenerated output polygon produced (vertices < 3).");
                }
                result.Add(output);
            }
            return errVal;
        }

        /// <summary>Needed to calculate the characteristics function of a simplex.</summary>
        private static float CalculateBeta(Vector2 point, Edge simplex, float coefficient)
        {
            var result = 0.0f;
            if (PointInSimplex(point, simplex))
            {
                result = coefficient;
            }
            if (PointOnLineSegment(Vector2.Zero, simplex.EdgeStart, point) ||
                PointOnLineSegment(Vector2.Zero, simplex.EdgeEnd, point))
            {
                result = 0.5f * coefficient;
            }
            return result;
        }

        /// <summary>Needed for sorting multiple intersections points on the same edge.</summary>
        private static float GetAlpha(Vector2 start, Vector2 end, Vector2 point)
        {
            return (point - start).LengthSquared() / (end - start).LengthSquared();
        }

        /// <summary>Returns the coefficient of a simplex.</summary>
        private static float CalculateSimplexCoefficient(Vector2 a, Vector2 b, Vector2 c)
        {
            var isLeft = Area(ref a, ref b, ref c);
            if (isLeft < 0f)
            {
                return -1f;
            }
            if (isLeft > 0f)
            {
                return 1f;
            }
            return 0f;
        }

        /// <summary>Winding number test for a point in a simplex.</summary>
        /// <param name="point">The point to be tested.</param>
        /// <param name="edge">The edge that the point is tested against.</param>
        /// <returns>False if the winding number is even and the point is outside the simplex and True otherwise.</returns>
        private static bool PointInSimplex(Vector2 point, Edge edge)
        {
            var polygon = new List<Vector2> {Vector2.Zero, edge.EdgeStart, edge.EdgeEnd};

            // Winding number.
            var wn = 0;

            // Iterate through polygon's edges.
            for (int i = 0, count = polygon.Count; i < count; i++)
            {
                // Get points
                var a = polygon[i];
                var b = polygon[(i + 1) % count];

                // Test if a point is directly on the edge
                var e = b - a;
                var area = a.X * (b.Y - point.Y) + b.X * (point.Y - a.Y) + point.X * (a.Y - b.Y);
// ReSharper disable CompareOfFloatsByEqualityOperator
                if (area == 0.0f &&
// ReSharper restore CompareOfFloatsByEqualityOperator
                    Vector2.Dot(point - a, e) >= 0.0f &&
                    Vector2.Dot(point - b, e) <= 0.0f)
                {
                    return false;
                }

                // Test edge for intersection with ray from point.
                if (a.Y <= point.Y)
                {
                    if (b.Y > point.Y && area > 0.0f)
                    {
                        ++wn;
                    }
                }
                else
                {
                    if (b.Y <= point.Y && area < 0.0f)
                    {
                        --wn;
                    }
                }
            }

            return wn != 0;
        }

        /// <summary>Tests if a point lies on a line segment.</summary>
        private static bool PointOnLineSegment(Vector2 start, Vector2 end, Vector2 point)
        {
            var segment = end - start;
// ReSharper disable CompareOfFloatsByEqualityOperator
            return Area(ref start, ref end, ref point) == 0.0f &&
// ReSharper restore CompareOfFloatsByEqualityOperator
                   Vector2.Dot(point - start, segment) >= 0.0f &&
                   Vector2.Dot(point - end, segment) <= 0.0f;
        }

        private static float Area(ref Vector2 a, ref Vector2 b, ref Vector2 c)
        {
            return a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y);
        }

        private static bool VectorEqual(Vector2 a, Vector2 b)
        {
            return (b - a).LengthSquared() <= ClipperEpsilonSquared;
        }

        /// <summary>Specifies an Edge. Edges are used to represent simplices in simplical chains</summary>
        private sealed class Edge : IEquatable<Edge>
        {
            public Edge(Vector2 edgeStart, Vector2 edgeEnd)
            {
                EdgeStart = edgeStart;
                EdgeEnd = edgeEnd;
            }

            public Vector2 EdgeStart { get; private set; }
            public Vector2 EdgeEnd { get; private set; }

            public Vector2 GetCenter()
            {
                return (EdgeStart + EdgeEnd) * 0.5f;
            }

            public static Edge operator -(Edge e)
            {
                return new Edge(e.EdgeEnd, e.EdgeStart);
            }

            public override bool Equals(Object value)
            {
                return value != null && Equals(value as Edge);
            }

            public bool Equals(Edge value)
            {
                return value != null && (VectorEqual(EdgeStart, value.EdgeStart) && VectorEqual(EdgeEnd, value.EdgeEnd));
            }

            public override int GetHashCode()
            {
                return EdgeStart.GetHashCode() ^ EdgeEnd.GetHashCode();
            }
        }
    }
}