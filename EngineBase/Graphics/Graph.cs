using System;
using System.Collections.Generic;
using System.Globalization;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Graphics
{
    /// <summary>
    /// A class for rendering graphs, based on float data.
    /// </summary>
    public sealed class Graph
    {
        #region Types

        /// <summary>
        /// Possible render types for graphs.
        /// </summary>
        public enum GraphType
        {
            /// <summary>
            /// Invalid type, won't render anything.
            /// </summary>
            None,

            /// <summary>
            /// Line graph, drawing each data source as a line.
            /// </summary>
            Line,

            /// <summary>
            /// Stacked area graph, drawing each data source as an area, where
            /// the areas are stacked on top of each other (in vertical space)
            /// to give a direct feedback of the sum of the individual data.
            /// </summary>
            StackedArea
        }

        /// <summary>
        /// Shortening types for displayed values.
        /// </summary>
        public enum UnitPrefixes
        {
            /// <summary>
            /// No prefix convention, won't shorten value display in captions. This
            /// may lead to text overflowing the set bounds.
            /// </summary>
            None,

            /// <summary>
            /// SI prefix convention will shorten value display in captions by
            /// dividing them by 1000, and using the according SI unit prefixes
            /// of k, M, G, T, P, E, Z, Y.
            /// </summary>
            SI,

            /// <summary>
            /// IEC prefix convention will shorten the display in captions by
            /// dividing them by 1024, and using the according IEC unit prefixes
            /// of Ki, Mi, Gi, Ti, Pi, Ei, Zi, Yi.
            /// </summary>
            IEC
        }

        #endregion

        #region Constants

        /// <summary>
        /// Padding on sides and between captions and graph.
        /// </summary>
        private const int Padding = 5;

        /// <summary>
        /// Height of area to use for text rendering.
        /// </summary>
        private const int VerticalCaptionSize = 16;

        /// <summary>
        /// Minimum width to use for graphs (actually more for captions).
        /// </summary>
        private const int MinGraphWidth = 230;

        /// <summary>
        /// Minimum height of the actual graph area.
        /// </summary>
        private const int MinGraphHeight = 20 + Padding * 2;

        /// <summary>
        /// SI unit prefixes, sorted by order of magnitude.
        /// </summary>
        private static readonly string[] SIPrefixes = new[] {"", "k", "M", "G", "T", "P", "E", "Z", "Y"};

        /// <summary>
        /// IEC unit prefixes, sorted by order of magnitude.
        /// </summary>
        private static readonly string[] IECPrefixes = new[] {"", "Ki", "Mi", "Gi", "Ti", "Pi", "Ei", "Zi", "Yi"};

        #endregion

        #region Properties

        /// <summary>
        /// The bounds in which to render the graph. This must be large enough
        /// to allow room for captions, i.e. this does not equal the area of
        /// the actual graph.
        /// </summary>
        public Microsoft.Xna.Framework.Rectangle Bounds { get; set; }

        /// <summary>
        /// How to render the graph.
        /// </summary>
        public GraphType Type { get; set; }

        /// <summary>
        /// Defines how to compute values to render. If a value <c>n > 1</c>
        /// is set, the rendered value will be the average over the last <c>n</c>
        /// data points. This will also mean the actually rendered data points
        /// will be <c>n - 1</c> less than actually available.
        /// </summary>
        public int Smoothing { get; set; }

        /// <summary>
        /// The unit prefix to use when shortening values for display. Defaults
        /// to SI prefix convention.
        /// </summary>
        public UnitPrefixes UnitPrefix { get; set; }

        /// <summary>
        /// The unit name of the values. Defaults to an empty string (unit less).
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// A title to show above the graph.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// A fixed maximum value, so as to not automatically scale the graph
        /// in vertical direction.
        /// </summary>
        public float? FixedMaximum { get; set; }

        /// <summary>
        /// The data provider we iterate to render the graph. Each enumerable
        /// represents an individual data set. The data sets should each be of
        /// equal length, and must be of the same unit.
        /// </summary>
        public IEnumerable<float>[] Data { get; set; }

        #endregion

        #region Fields
        
        /// <summary>
        /// Used to render overall outline and outline around graph area.
        /// </summary>
        private readonly Rectangle _outlines;

        /// <summary>
        /// Used to render overall background.
        /// </summary>
        private readonly GradientRectangle _background;

        /// <summary>
        /// Font used to render graph captions.
        /// </summary>
        private readonly SpriteFont _font;

        /// <summary>
        /// Used to render the graph captions font.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// The texture we use to (manually...) render the curves.
        /// </summary>
        private Texture2D _graphCanvas;

        /// <summary>
        /// Actual image data we manipulate.
        /// </summary>
        private Color[] _graphImageData;

        /// <summary>
        /// Use a maximum from recent time to interpolate from to avoid
        /// sudden scale jumps.
        /// </summary>
        private float _recentMax = 1;

        /// <summary>
        /// Reused list for data accumulation to avoid garbage.
        /// </summary>
        private readonly List<float> _points;

        /// <summary>
        /// Reused data accumulator to avoid garbage.
        /// </summary>
        private float[][] _buildCurves;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Graph"/> class.
        /// </summary>
        /// <param name="content">The content manager used to load assets.</param>
        /// <param name="graphics">The graphics device to draw to.</param>
        public Graph(ContentManager content, GraphicsDevice graphics)
        {
            _background = new GradientRectangle(content, graphics);
            _background.SetGradients(new[]
                                     {
                                         Color.Black * 0.5f,
                                         Color.Black * 0.8f
                                     });
            _outlines = new Rectangle(content, graphics) {Color = Color.DarkGray};
            _spriteBatch = new SpriteBatch(graphics);
            _font = content.Load<SpriteFont>("Fonts/GraphCaptions");

            Type = GraphType.Line;
            UnitPrefix = UnitPrefixes.SI;
            _points = new List<float>();
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Render the graph with its current settings.
        /// </summary>
        public void Draw()
        {
            var minSize = ComputeMinimumSize();
            if (Bounds.Width <= minSize.Width || Bounds.Height <= minSize.Height)
            {
                Bounds = minSize;
            }
            DrawBackground();

            float min, max, average;
            var anchors = BuildCurves(out min, out max, out average);

            if (anchors != null)
            {
                switch (Type)
                {
                    case GraphType.Line:
                        DrawLines(anchors, min, max, average);
                        break;
                    case GraphType.StackedArea:
                        DrawStackedAreas(anchors);
                        break;
                }
            }

            DrawCaptions(min, max, average);
        }

        private void DrawBackground()
        {
            // Draw overall background.
            _background.SetSize(Bounds.Width, Bounds.Height);
            _background.SetCenter(Bounds.Center.X, Bounds.Center.Y);
            _background.Draw();

            // Draw outer outline.
            _outlines.SetSize(Bounds.Width, Bounds.Height);
            _outlines.SetCenter(Bounds.Center.X, Bounds.Center.Y);
            _outlines.Draw();

            // Draw inner outline for actual graphs.
            var graphBounds = ComputeGraphBounds();
            _outlines.SetSize(graphBounds.Width, graphBounds.Height);
            _outlines.SetCenter(graphBounds.Center.X, graphBounds.Center.Y);
            _outlines.Draw();
        }

        /// <summary>
        /// Draws the captions for the graph.
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <param name="average">The average value.</param>
        private void DrawCaptions(float min, float max, float average)
        {
            _spriteBatch.Begin();

            if (!string.IsNullOrWhiteSpace(Title))
            {
                _spriteBatch.DrawString(_font, Title, new Vector2(Bounds.X + Padding, Bounds.Y + Padding), Color.White);
            }

            var maxText = "max: " + Format(max);
            var minText = "min: " + Format(min);

            var avgText = "avg: " + Format(average);

            _spriteBatch.DrawString(_font, maxText, new Vector2(Bounds.X + Padding, Bounds.Bottom - Padding - VerticalCaptionSize * 2), Color.White);
            _spriteBatch.DrawString(_font, minText, new Vector2(Bounds.X + Padding, Bounds.Bottom - Padding - VerticalCaptionSize), Color.White);

            _spriteBatch.DrawString(_font, avgText, new Vector2(Bounds.X + Padding + Bounds.Width / 2, Bounds.Bottom - Padding - VerticalCaptionSize * 2), Color.White);

            _spriteBatch.End();
        }

        /// <summary>
        /// Draws lines for each data set.
        /// </summary>
        /// <param name="anchors">The anchors to draw the lines through.</param>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <param name="average">The average value.</param>
        private void DrawLines(IEnumerable<float[]> anchors, float min, float max, float average)
        {
            var graphBounds = ComputeGraphBounds();
            graphBounds.Height -= Padding * 2;
            graphBounds.Y += Padding;
            if (_graphCanvas == null ||
                _graphCanvas.Width != graphBounds.Width ||
                _graphCanvas.Height != graphBounds.Height)
            {
                if (_graphCanvas != null)
                {
                    _graphCanvas.Dispose();
                }
                _graphCanvas = new Texture2D(_spriteBatch.GraphicsDevice,
                                             graphBounds.Width,
                                             graphBounds.Height);
                _graphImageData = new Color[graphBounds.Width * graphBounds.Height];
            }

            Vector2 position;
            position.X = graphBounds.X;
            position.Y = graphBounds.Y;

            _spriteBatch.Begin();

            foreach (var data in anchors)
            {
                if (data.Length <= Smoothing)
                {
                    continue;
                }
                var w = (data.Length - 1) / (float)_graphCanvas.Width;
                var lastY = 0f;
                for (var x = 0; x < _graphCanvas.Width; x++)
                {
                    var bucket = x * w;
                    var a = (int)System.Math.Floor(bucket);
                    var b = (int)System.Math.Ceiling(bucket);
                    float targetY;
                    if (a == b)
                    {
                        targetY = data[a];
                    }
                    else
                    {
                        var wa = 1 - System.Math.Abs(a - bucket);
                        var wb = 1 - System.Math.Abs(b - bucket);
                        var da = data[System.Math.Max(0, a)];
                        var db = data[System.Math.Min(data.Length - 1, b)];
                        targetY = da * wa + db * wb;
                    }
                    targetY = _graphCanvas.Height - 2 - targetY * (_graphCanvas.Height - 2);
                    var minY = System.Math.Max(0, _graphCanvas.Height - min / _recentMax * _graphCanvas.Height);
                    var maxY = System.Math.Max(0, _graphCanvas.Height - max / _recentMax * _graphCanvas.Height);
                    var avgY = System.Math.Max(0, _graphCanvas.Height - average / _recentMax * _graphCanvas.Height);
                    for (var y = 0; y < _graphCanvas.Height; y++)
                    {
                        // Draw line.
                        var local = y - lastY;
                        var alpha = System.Math.Max(0, System.Math.Min(1, System.Math.Max(0, 1.5f - System.Math.Abs(local - 0.5f))));

                        // Draw min/max/average lines
                        local = y - minY;
                        alpha += 0.3f * System.Math.Max(0, 1.8f - 2 * System.Math.Abs(local - 0.5f));
                        if (!float.IsInfinity(max))
                        {
                            local = y - maxY;
                            alpha += 0.3f * System.Math.Max(0, 1.8f - 2 * System.Math.Abs(local - 0.5f));
                        }
                        if (!float.IsInfinity(average))
                        {
                            local = y - avgY;
                            alpha += 0.3f * System.Math.Max(0, 1.8f - 2 * System.Math.Abs(local - 0.5f));
                        }
                        
                        // Set final color.
                        _graphImageData[x + y * _graphCanvas.Width] = Color.White * alpha;
                    }
                    lastY = targetY;
                }

                _graphCanvas.SetData(_graphImageData);
                _spriteBatch.Draw(_graphCanvas, position, Color.White);
            }

            _spriteBatch.End();
        }

        private void DrawStackedAreas(IEnumerable<float[]> anchors)
        {
            // TODO...
            throw new NotImplementedException();
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Computes the minimum size of the graph.
        /// </summary>
        /// <returns></returns>
        private Microsoft.Xna.Framework.Rectangle ComputeMinimumSize()
        {
            Microsoft.Xna.Framework.Rectangle r;
            r.X = Bounds.X;
            r.Y = Bounds.Y;
            const int mw = Padding * 2 + MinGraphWidth;
            var mh = Padding * 3 + VerticalCaptionSize * 2 + MinGraphHeight + (string.IsNullOrWhiteSpace(Title) ? 0 : VerticalCaptionSize + Padding);
            r.Width = System.Math.Max(Bounds.Width, mw);
            r.Height = System.Math.Max(Bounds.Height, mh);
            return r;
        }

        /// <summary>
        /// Computes the current graph bounds (actual graph area).
        /// </summary>
        /// <returns></returns>
        private Microsoft.Xna.Framework.Rectangle ComputeGraphBounds()
        {
            Microsoft.Xna.Framework.Rectangle r;
            r.X = Padding;
            r.Y = Padding + (string.IsNullOrWhiteSpace(Title) ? 0 : VerticalCaptionSize + Padding);
            r.Width = Bounds.Width - Padding * 2;
            r.Height = Bounds.Height - r.Y - VerticalCaptionSize * 2 - Padding * 2;
            r.X += Bounds.X;
            r.Y += Bounds.Y;
            return r;
        }

        /// <summary>
        /// Process data to build actual data points to draw the graph through,
        /// in relative values.
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        /// <param name="average">The average value.</param>
        /// <returns></returns>
        private IEnumerable<float[]> BuildCurves(out float min, out float max, out float average)
        {
            // Skip if there is no data.
            if (Data == null || Data.Length == 0)
            {
                min = max = average = 0;
                return null;
            }

            // Initialize values to impossible values to override them.
            min = float.PositiveInfinity;
            max = float.NegativeInfinity;

            // And start the average at zero, of course.
            average = 0;
            var count = 0;

            // Allocate data for actual data points.
            if (_buildCurves == null || _buildCurves.Length != Data.Length)
            {
                _buildCurves = new float[Data.Length][];   
            }

            // Iterate each data collection.
            for (int i = 0, j = Data.Length; i < j; i++)
            {
                // Skip invalid data sources.
                if (Data[i] == null)
                {
                    if (_buildCurves[i] == null || _buildCurves[i].Length != 0)
                    {
                        _buildCurves[i] = new float[0];
                    }
                    continue;
                }

                // Otherwise start walking over our data source.
                var movingAverage = new FloatSampling(Smoothing + 1);
                foreach (var data in Data[i])
                {
                    // Build average based on smoothing parameter.
                    movingAverage.Put(data);

                    // Store value in our data point list.
                    _points.Add((float)movingAverage.Mean());

                    // Update our extrema.
                    if (data < min)
                    {
                        min = data;
                    }
                    if (data > max)
                    {
                        max = data;
                    }

                    average += data;
                    ++count;
                }

                // Strip as many items as we have to based on smoothing (to
                // get rid of starting entries which cannot be smoothed).
                for (var k = 0; k < Smoothing - 1 && _points.Count > 0; k++)
                {
                    _points.RemoveAt(0);
                }

                // Convert list to array and store it.
                if (_buildCurves[i] == null || _buildCurves[i].Length != _points.Count)
                {
                    _buildCurves[i] = new float[_points.Count];
                }
                _points.CopyTo(_buildCurves[i]);

                // Clear list for next iteration.
                _points.Clear();
            }

            // Adjust maximum.
            if (FixedMaximum.HasValue)
            {
                _recentMax = FixedMaximum.Value;
            }
            else if (!float.IsInfinity(max))
            {
                if (max > _recentMax)
                {
                    _recentMax = MathHelper.Lerp(_recentMax, max, 0.1f);
                }
                else
                {
                    _recentMax = MathHelper.Lerp(_recentMax, max, 0.01f);
                }
            }

            // Scale all curves to fit in the graphing area.
            foreach (var data in _buildCurves)
            {
                for (int i = 0, j = data.Length; i < j; i++)
                {
                    data[i] /= _recentMax;
                }
            }

            // Adjust average.
            average /= count;

            // Return point list with actual coordinates for points to render.
            return _buildCurves;
        }

        /// <summary>
        /// Formats a value based on the set unit settings for rendering.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>The formatted string representing the value.</returns>
        private string Format(float value)
        {
            // Not much to do if it's infinity.
            if (float.IsInfinity(value) || float.IsNaN(value))
            {
                return "N/A";
            }

            // See how to process values.
            string[] prefixes;
            float divisor;
            switch(UnitPrefix)
            {
                case UnitPrefixes.SI:
                    prefixes = SIPrefixes;
                    divisor = 1000;
                    break;
                case UnitPrefixes.IEC:
                    prefixes = IECPrefixes;
                    divisor = 1024;
                    break;
                default:
                    // Return default formatted string without unit prefix.
                    return string.Format(CultureInfo.InvariantCulture, "{0:0.00} {1}", value, Unit);
            }

            // Divide until we don't have to any more, or don't know the
            // unit prefix.
            var prefix = 0;
            while (value > divisor && prefix < prefixes.Length)
            {
                value /= divisor;
                ++prefix;
            }

            // Return formatted string.
            return string.Format(CultureInfo.InvariantCulture, "{0:0.00} {1}{2}", value, prefixes[prefix], Unit);
        }

        #endregion
    }
}
