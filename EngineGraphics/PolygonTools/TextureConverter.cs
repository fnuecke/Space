using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Graphics.PolygonTools
{
    /// <summary>
    ///     This class can be used to analyze textures and extract polygons from them. This was taken from the FarSeer
    ///     Physics Engine, to which is was a contribution by 'Sickbattery'.
    /// </summary>
    public static class TextureConverter
    {
        // Note: A bool array would probably speed up the algorithm.
        private static readonly int[,] ClosePixels = new[,]
        {
            {-1, -1}, {0, -1}, {1, -1}, {1, 0}, {1, 1}, {0, 1}, {-1, 1}, {-1, 0}
        };

        /// <summary>Cache for already analyzed textures.</summary>
        private static readonly Dictionary<string, List<CacheEntry>> Cache = new Dictionary<string, List<CacheEntry>>();

        private sealed class CacheEntry
        {
            public float HullTolerance;
            public byte AlphaTolerance;
            public bool MultiPartDetection;
            public List<List<Vector2>> Result;
        }

        /// <summary>
        ///     Clears any cached results from previous
        ///     <see cref="DetectVertices(Microsoft.Xna.Framework.Graphics.Texture2D,float,byte,bool,string)"/> calls.
        /// </summary>
        public static void ClearCache()
        {
            Cache.Clear();
        }
        
        /// <summary>Detects the vertices of the supplied texture.</summary>
        /// <remarks>This caches results for a given parameterization for better performance.</remarks>
        /// <param name="texture">The texture.</param>
        /// <param name="hullTolerance">The hull tolerance.</param>
        /// <param name="alphaTolerance">The alpha tolerance.</param>
        /// <param name="multiPartDetection">
        ///     if set to <c>true</c> it will perform multi part detection.
        /// </param>
        /// <param name="textureName">The name of the texture, for caching.</param>
        /// <returns></returns>
        public static List<List<Vector2>> DetectVertices(
            Texture2D texture,
            float hullTolerance = 1.5f,
            byte alphaTolerance = 20,
            bool multiPartDetection = false,
            string textureName = null)
        {
            if (!string.IsNullOrWhiteSpace(textureName))
            {
                if (Cache.ContainsKey(textureName))
                {
                    foreach (var entry in Cache[textureName])
                    {
// ReSharper disable CompareOfFloatsByEqualityOperator Must be exact same to allow use of cached entries.
                        if (entry.HullTolerance == hullTolerance &&
// ReSharper restore CompareOfFloatsByEqualityOperator
                            entry.AlphaTolerance == alphaTolerance &&
                            entry.MultiPartDetection == multiPartDetection)
                        {
                            return entry.Result;
                        }
                    }
                }
                else
                {
                    Cache[textureName] = new List<CacheEntry>();
                }
            }

            var data = new uint[texture.Width * texture.Height];
            texture.GetData(data);
            var result = DetectVertices(
                data, texture.Width, texture.Height, hullTolerance, alphaTolerance, multiPartDetection);

            if (!string.IsNullOrWhiteSpace(textureName))
            {
                Cache[textureName].Add(
                    new CacheEntry
                    {
                        HullTolerance = hullTolerance,
                        AlphaTolerance = alphaTolerance,
                        MultiPartDetection = multiPartDetection,
                        Result = result
                    }
                    );
            }

            return result;
        }

        /// <summary>Detects the vertices of the supplied texture data.</summary>
        /// <param name="data">The texture data.</param>
        /// <param name="width">The texture width.</param>
        /// <param name="height">The texture height.</param>
        /// <param name="hullTolerance">The hull tolerance.</param>
        /// <param name="alphaTolerance">The alpha tolerance.</param>
        /// <param name="multiPartDetection">
        ///     if set to <c>true</c> it will perform multi part detection.
        /// </param>
        /// <returns></returns>
        public static List<List<Vector2>> DetectVertices(
            uint[] data,
            int width,
            int height,
            float hullTolerance = 1.5f,
            byte alphaTolerance = 20,
            bool multiPartDetection = false)
        {
            var assistance = new PolygonCreationAssistance(data, width, height)
            {
                HullTolerance = hullTolerance,
                AlphaTolerance = alphaTolerance,
                MultipartDetection = multiPartDetection
            };
            return DetectVertices(ref assistance);
        }

        private static List<List<Vector2>> DetectVertices(ref PolygonCreationAssistance assistance)
        {
            // Check the array we just got.
            Debug.Assert(assistance.IsValid(), "Sizes don't match: Color array must contain texture width * texture height elements.");

            var polygons = new List<List<Vector2>>();
            Vector2? polygonEntrance = null;

            do
            {
                List<Vector2> polygon;
                if (polygons.Count == 0)
                {
                    polygon = CreateSimplePolygon(assistance, Vector2.Zero, Vector2.Zero);

                    if (polygon != null && polygon.Count > 2)
                    {
                        polygonEntrance = GetTopMostVertex(polygon);
                    }
                }
                else if (polygonEntrance.HasValue)
                {
                    polygon = CreateSimplePolygon(
                        assistance,
                        polygonEntrance.Value,
                        new Vector2(polygonEntrance.Value.X - 1f, polygonEntrance.Value.Y));
                }
                else
                {
                    break;
                }

                var done = true;

                if (polygon != null && polygon.Count > 2)
                {
                    polygons.Add(polygon);

                    if (assistance.MultipartDetection)
                    {
                        // 1:  95 / 151
                        // 2: 232 / 252
                        while (GetNextHullEntrance(assistance, polygonEntrance.Value, out polygonEntrance))
                        {
                            var inPolygon = false;
                            foreach (var vertices in polygons)
                            {
                                if (InPolygon(assistance, vertices, polygonEntrance.Value))
                                {
                                    inPolygon = true;
                                    break;
                                }
                            }

                            if (!inPolygon)
                            {
                                done = false;
                                break;
                            }
                        }
                    }
                }

                if (done)
                {
                    break;
                }
            } while (true);

            return polygons;
        }

        private static bool DistanceToHullAcceptable(PolygonCreationAssistance assistance, IList<Vector2> polygon, Vector2 point)
        {
            if (polygon == null || polygon.Count <= 2)
            {
                return false;
            }

            var lastVertex = polygon[polygon.Count - 1];
            foreach (var vertex in polygon)
            {
                if (DistanceBetweenPointAndLineSegment(point, vertex, lastVertex) <= assistance.HullTolerance ||
                    Vector2.Distance(point, vertex) <= assistance.HullTolerance)
                {
                    return false;
                }
                lastVertex = vertex;
            }

            return true;
        }

        private static bool InPolygon(PolygonCreationAssistance assistance, IList<Vector2> polygon, Vector2 point)
        {
            if (!DistanceToHullAcceptable(assistance, polygon, point))
            {
                return true;
            }

            var edges = GetCrossingEdges(polygon, (int) point.Y);
            if (edges.Count > 0 && edges.Count % 2 == 0)
            {
                for (var i = 0; i < edges.Count; i += 2)
                {
                    if (edges[i].CrossingPoint.X <= point.X && edges[i + 1].CrossingPoint.X >= point.X)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Vector2? GetTopMostVertex(IList<Vector2> vertices)
        {
            var topMostValue = float.MaxValue;
            Vector2? topMost = null;

            for (var i = 0; i < vertices.Count; i++)
            {
                if (topMostValue > vertices[i].Y)
                {
                    topMostValue = vertices[i].Y;
                    topMost = vertices[i];
                }
            }

            return topMost;
        }

        private static List<CrossingEdgeInfo> GetCrossingEdges(IList<Vector2> polygon, int checkLine)
        {
            var edges = new List<CrossingEdgeInfo>();

            if (polygon.Count < 2)
            {
                return edges;
            }

            var lastVertex = polygon[polygon.Count - 1];
            for (var i = 0; i < polygon.Count; i++)
            {
                var vertex = polygon[i];

                if ((vertex.Y >= checkLine && lastVertex.Y <= checkLine) ||
                    (vertex.Y <= checkLine && lastVertex.Y >= checkLine))
                {
// ReSharper disable CompareOfFloatsByEqualityOperator
                    if (vertex.Y != lastVertex.Y)
// ReSharper restore CompareOfFloatsByEqualityOperator
                    {
                        var addCrossingPoint = true;
                        var slope = lastVertex - vertex;

// ReSharper disable CompareOfFloatsByEqualityOperator
                        if (vertex.Y == checkLine)
// ReSharper restore CompareOfFloatsByEqualityOperator
                        {
                            var nextVertex = polygon[(i + 1) % polygon.Count];
                            var slopePreview = vertex - nextVertex;

                            if (slope.Y > 0)
                            {
                                addCrossingPoint = (slopePreview.Y <= 0);
                            }
                            else
                            {
                                addCrossingPoint = (slopePreview.Y >= 0);
                            }
                        }

                        if (addCrossingPoint)
                        {
                            var crossingPoint = new Vector2((checkLine - vertex.Y) / slope.Y * slope.X + vertex.X, checkLine);
                            edges.Add(new CrossingEdgeInfo(crossingPoint));
                        }
                    }
                }
                lastVertex = vertex;
            }

            edges.Sort();
            return edges;
        }

        private static List<Vector2> CreateSimplePolygon(PolygonCreationAssistance assistance, Vector2 entrance, Vector2 last)
        {
            var entranceFound = false;
            var endOfHull = false;

            var polygon = new List<Vector2>(32);
            var hullArea = new List<Vector2>(32);
            var endOfHullArea = new List<Vector2>(32);

            var current = Vector2.Zero;

            // Get the entrance point. //todo: test all possibilities
            if (entrance == Vector2.Zero || !assistance.InBounds(entrance))
            {
                entranceFound = GetHullEntrance(assistance, out entrance);

                if (entranceFound)
                {
                    current = new Vector2(entrance.X - 1f, entrance.Y);
                }
            }
            else
            {
                if (assistance.IsSolid(entrance))
                {
                    if (IsNearPixel(assistance, ref entrance, ref last))
                    {
                        current = last;
                        entranceFound = true;
                    }
                    else
                    {
                        Vector2 temp;
                        if (SearchNearPixels(assistance, false, entrance, out temp))
                        {
                            current = temp;
                            entranceFound = true;
                        }
                    }
                }
            }

            if (entranceFound)
            {
                polygon.Add(entrance);
                hullArea.Add(entrance);

                var next = entrance;

                do
                {
                    // Search in the pre vision list for an outstanding point.
                    Vector2 outstanding;
                    if (SearchForOutstandingVertex(hullArea, assistance.HullTolerance, out outstanding))
                    {
                        if (endOfHull)
                        {
                            // We have found the next pixel, but is it on the last bit of the hull?
                            if (endOfHullArea.Contains(outstanding))
                            {
                                // Indeed.
                                polygon.Add(outstanding);
                            }

                            // That's enough, quit.
                            break;
                        }

                        // Add it and remove all vertices that don't matter anymore
                        // (all the vertices before the outstanding).
                        polygon.Add(outstanding);
                        hullArea.RemoveRange(0, hullArea.IndexOf(outstanding));
                    }

                    // Last point gets current and current gets next. Our little spider is moving forward on the hull ;).
                    last = current;
                    current = next;

                    // Get the next point on hull.
                    if (GetNextHullPoint(assistance, ref last, ref current, out next))
                    {
                        // Add the vertex to a hull pre vision list.
                        hullArea.Add(next);
                    }
                    else
                    {
                        // Quit
                        break;
                    }

                    if (next == entrance && !endOfHull)
                    {
                        // It's the last bit of the hull, search on and exit at next found vertex.
                        endOfHull = true;
                        endOfHullArea.AddRange(hullArea);
                    }
                } while (true);
            }

            return polygon;
        }

        private static bool SearchNearPixels(PolygonCreationAssistance assistance, bool searchingForSolidPixel, Vector2 current, out Vector2 foundPixel)
        {
            for (var i = 0; i < 8; i++)
            {
                var x = (int) current.X + ClosePixels[i, 0];
                var y = (int) current.Y + ClosePixels[i, 1];

                if (!searchingForSolidPixel ^ assistance.IsSolid(x, y))
                {
                    foundPixel = new Vector2(x, y);
                    return true;
                }
            }
            foundPixel = Vector2.Zero;
            return false;
        }

        private static bool IsNearPixel(PolygonCreationAssistance assistance, ref Vector2 current, ref Vector2 near)
        {
            for (var i = 0; i < 8; i++)
            {
                var x = (int) current.X + ClosePixels[i, 0];
                var y = (int) current.Y + ClosePixels[i, 1];

                if (x >= 0 && x <= assistance.Width && y >= 0 && y <= assistance.Height &&
                    x == (int) near.X && y == (int) near.Y)
                {
                        return true;
                }
            }
            return false;
        }

        private static bool GetHullEntrance(PolygonCreationAssistance assistance, out Vector2 entrance)
        {
            // Search for first solid pixel.
            for (var y = 0; y <= assistance.Height; y++)
            {
                for (var x = 0; x <= assistance.Width; x++)
                {
                    if (assistance.IsSolid(x, y))
                    {
                        entrance = new Vector2(x, y);
                        return true;
                    }
                }
            }
            entrance = Vector2.Zero;
            return false;
        }

        private static bool GetNextHullEntrance(PolygonCreationAssistance assistance, Vector2 start, out Vector2? entrance)
        {
            // Search for first solid pixel.
            var size = assistance.Height * assistance.Width;

            var foundTransparent = false;

            for (var i = (int) start.X + (int) start.Y * assistance.Width; i <= size; i++)
            {
                if (assistance.IsSolid(i))
                {
                    if (foundTransparent)
                    {
                        var x = i % assistance.Width;
                        entrance = new Vector2(x, (i - x) / (float) assistance.Width);
                        return true;
                    }
                }
                else
                {
                    foundTransparent = true;
                }
            }

            // If there are no solid pixels.
            entrance = null;
            return false;
        }

        private static bool GetNextHullPoint(
            PolygonCreationAssistance assistance,
            ref Vector2 last,
            ref Vector2 current,
            out Vector2 next)
        {
            var firstPixel = GetIndexOfFirstPixelToCheck(last, current);
            for (var i = 0; i < 8; i++)
            {
                var pixel = (firstPixel + i) % 8;
                var x = (int) current.X + ClosePixels[pixel, 0];
                var y = (int) current.Y + ClosePixels[pixel, 1];

                if (x >= 0 && x < assistance.Width && y >= 0 && y <= assistance.Height)
                {
                    if (assistance.IsSolid(x, y))
                    {
                        next = new Vector2(x, y);
                        return true;
                    }
                }
            }

            next = Vector2.Zero;
            return false;
        }

        private static bool SearchForOutstandingVertex(
            IList<Vector2> hullArea, float hullTolerance, out Vector2 outstanding)
        {
            var outstandingResult = Vector2.Zero;
            var found = false;

            if (hullArea.Count > 2)
            {
                var hullAreaLastPoint = hullArea.Count - 1;

                var tempVector2 = hullArea[0];
                var tempVector3 = hullArea[hullAreaLastPoint];

                // Search between the first and last hull point.
                for (var i = 1; i < hullAreaLastPoint; i++)
                {
                    var tempVector1 = hullArea[i];

                    // Check if the distance is over the one that's tolerable.
                    if (DistanceBetweenPointAndLineSegment(tempVector1, tempVector2, tempVector3) >=
                        hullTolerance)
                    {
                        outstandingResult = hullArea[i];
                        found = true;
                        break;
                    }
                }
            }

            outstanding = outstandingResult;
            return found;
        }

        private static int GetIndexOfFirstPixelToCheck(Vector2 last, Vector2 current)
        {
            // .: pixel
            // l: last position
            // c: current position
            // f: first pixel for next search

            // f . .
            // l c .
            // . . .

            //Calculate in which direction the last move went and decide over the next first pixel.
            switch ((int) (current.X - last.X))
            {
                case 1:
                    switch ((int) (current.Y - last.Y))
                    {
                        case 1:
                            return 1;

                        case 0:
                            return 0;

                        case -1:
                            return 7;
                    }
                    break;

                case 0:
                    switch ((int) (current.Y - last.Y))
                    {
                        case 1:
                            return 2;

                        case -1:
                            return 6;
                    }
                    break;

                case -1:
                    switch ((int) (current.Y - last.Y))
                    {
                        case 1:
                            return 3;

                        case 0:
                            return 4;

                        case -1:
                            return 5;
                    }
                    break;
            }

            return 0;
        }

        private static float DistanceBetweenPointAndLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            var v = lineEnd - lineStart;
            var w = point - lineStart;

            var c1 = Vector2.Dot(w, v);
            if (c1 <= 0)
            {
                return Vector2.Distance(point, lineStart);
            }

            var c2 = Vector2.Dot(v, v);
            if (c2 <= c1)
            {
                return Vector2.Distance(point, lineEnd);
            }

            var b = c1 / c2;
            var pointOnLine = lineStart + v * b;

            return Vector2.Distance(point, pointOnLine);
        }

        private sealed class CrossingEdgeInfo : IComparable
        {
            public Vector2 CrossingPoint;

            public CrossingEdgeInfo(Vector2 crossingPoint)
            {
                CrossingPoint = crossingPoint;
            }

            public int CompareTo(object obj)
            {
                var info = (CrossingEdgeInfo) obj;

                if (CrossingPoint.X < info.CrossingPoint.X)
                {
                    return -1;
                }
                else if (CrossingPoint.X > info.CrossingPoint.X)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private struct PolygonCreationAssistance
        {
            public byte AlphaTolerance
            {
                get { return _alphaTolerance; }
                set
                {
                    _alphaTolerance = value;
                    _alphaToleranceRealValue = (uint) value << 24;
                }
            }

            public float HullTolerance
            {
                get { return _hullTolerance; }
                set
                {
                    var hullTolerance = value;

                    if (hullTolerance > 8f)
                    {
                        hullTolerance = 8f;
                    }
                    if (hullTolerance < 0.9f)
                    {
                        hullTolerance = 0.9f;
                    }

                    _hullTolerance = hullTolerance;
                }
            }

            public readonly int Width;
            public readonly int Height;
            public bool MultipartDetection;

            private readonly uint[] _data;
            private byte _alphaTolerance;
            private uint _alphaToleranceRealValue;
            private float _hullTolerance;

            public PolygonCreationAssistance(uint[] data, int width, int height) : this()
            {
                Width = width;
                Height = height;
                MultipartDetection = false;

                _data = data;

                AlphaTolerance = 20;
                HullTolerance = 1.5f;
            }

            public bool IsSolid(Vector2 pixel)
            {
                return IsSolid((int) pixel.X, (int) pixel.Y);
            }

            public bool IsSolid(int x, int y)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    return ((_data[x + y * Width] & 0xFF000000) >= _alphaToleranceRealValue);
                }

                return false;
            }

            public bool IsSolid(int index)
            {
                if (index >= 0 && index < Width * Height)
                {
                    return ((_data[index] & 0xFF000000) >= _alphaToleranceRealValue);
                }

                return false;
            }

            public bool InBounds(Vector2 coordinate)
            {
                return (coordinate.X >= 0f && coordinate.X < Width && coordinate.Y >= 0f && coordinate.Y < Height);
            }

            public bool IsValid()
            {
                if (_data != null && _data.Length > 0)
                {
                    return _data.Length == Width * Height;
                }

                return false;
            }
        }
    }
}