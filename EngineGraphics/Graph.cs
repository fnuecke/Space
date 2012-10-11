using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Engine.Math;
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

        /// <summary>
        /// For garbage free string formatting.
        /// </summary>
        private static readonly char[] Digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

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
        public int Smoothing
        {
            get { return _smoothing; }
            set
            {
                _movingAverage = new FloatSampling(value + 1);
                _smoothing = value;
            }
        }

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
        /// The data provider we iterate to render the graph.
        /// </summary>
        public IEnumerable<float> Data { set { _enumerator = value.GetEnumerator(); } }

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
        /// The reused enumerator.
        /// </summary>
        private IEnumerator<float> _enumerator;

        /// <summary>
        /// Use a maximum from recent time to interpolate from to avoid
        /// sudden scale jumps.
        /// </summary>
        private float _recentMax = 1;

        /// <summary>
        /// The smoothing to use (how many values to average over).
        /// </summary>
        private int _smoothing;

        /// <summary>
        /// The sampler to use to generate the moving average.
        /// </summary>
        private FloatSampling _movingAverage = new FloatSampling(1);

        /// <summary>
        /// Reused data accumulator to avoid garbage.
        /// </summary>
        private readonly List<float> _points;

        /// <summary>
        /// The most recent min, max, average and current value (from last update).
        /// </summary>
        private float _min, _max, _average, _now;

        /// <summary>
        /// String builder used to format texts to be displayed.
        /// </summary>
        private readonly StringBuilder _formatter = new StringBuilder(32);

        /// <summary>
        /// Timer we use to render only every now and then.
        /// </summary>
        private readonly Stopwatch _renderTimer = new Stopwatch();

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

            _renderTimer.Start();
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

            bool update = _renderTimer.ElapsedMilliseconds > 100;

            if (update)
            {
                BuildCurves();
                _renderTimer.Restart();
            }

            if (_points.Count > 0)
            {
                var graphBounds = ComputeGraphBounds();
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

                if (update)
                {
                    switch (Type)
                    {
                        case GraphType.Line:
                            DrawLines();
                            break;
                        case GraphType.StackedArea:
                            break;
                    }
                }

                Vector2 position;
                position.X = graphBounds.X;
                position.Y = graphBounds.Y;

                _spriteBatch.Begin();
                _spriteBatch.Draw(_graphCanvas, position, Color.White);
                _spriteBatch.End();
            }

            DrawCaptions();
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
            graphBounds.Height += Padding * 2;
            graphBounds.Y -= Padding;
            _outlines.SetSize(graphBounds.Width, graphBounds.Height);
            _outlines.SetCenter(graphBounds.Center.X, graphBounds.Center.Y);
            _outlines.Draw();
        }

        /// <summary>
        /// Draws the captions for the graph.
        /// </summary>
        private void DrawCaptions()
        {
            _spriteBatch.Begin();

            Vector2 position;
            position.X = Bounds.X + Padding;
            position.Y = Bounds.Y + Padding;

            if (!string.IsNullOrWhiteSpace(Title))
            {
                _spriteBatch.DrawString(_font, Title, position, Color.White);
            }

            position.X = Bounds.X + Padding;
            position.Y = Bounds.Bottom - Padding - VerticalCaptionSize * 2;
            _spriteBatch.DrawString(_font, Format("max: ", _max), position, Color.White);
            position.X = Bounds.X + Padding;
            position.Y = Bounds.Bottom - Padding - VerticalCaptionSize;
            _spriteBatch.DrawString(_font, Format("min: ", _min), position, Color.White);

            position.X = Bounds.X + Padding + Bounds.Width / 2;
            position.Y = Bounds.Bottom - Padding - VerticalCaptionSize * 2;
            _spriteBatch.DrawString(_font, Format("avg: ", _average), position, Color.White);
            position.X = Bounds.X + Padding + Bounds.Width / 2;
            position.Y = Bounds.Bottom - Padding - VerticalCaptionSize;
            _spriteBatch.DrawString(_font, Format("now: ", _now), position, Color.White);

            _spriteBatch.End();
        }

        /// <summary>
        /// Draws lines for each data set.
        /// </summary>
        private void DrawLines()
        {
            var w = (_points.Count - 1) / (float)_graphCanvas.Width;
            var lastY = 0f;
            for (var x = 0; x < _graphCanvas.Width; x++)
            {
                var bucket = x * w;
                var a = (int)System.Math.Floor(bucket);
                var b = (int)System.Math.Ceiling(bucket);
                float targetY;
                if (a == b)
                {
                    targetY = _points[a];
                }
                else
                {
                    var wa = 1 - System.Math.Abs(a - bucket);
                    var wb = 1 - System.Math.Abs(b - bucket);
                    var da = _points[System.Math.Max(0, a)];
                    var db = _points[System.Math.Min(_points.Count - 1, b)];
                    targetY = da * wa + db * wb;
                }
                targetY = _graphCanvas.Height - 2 - targetY * (_graphCanvas.Height - 2);
                var minY = System.Math.Max(0, _graphCanvas.Height - _min / _recentMax * _graphCanvas.Height);
                var maxY = System.Math.Max(0, _graphCanvas.Height - _max / _recentMax * _graphCanvas.Height);
                var avgY = System.Math.Max(0, _graphCanvas.Height - _average / _recentMax * _graphCanvas.Height);
                for (var y = 0; y < _graphCanvas.Height; y++)
                {
                    // Draw line.
                    var local = y - lastY;
                    var alpha = System.Math.Max(0, System.Math.Min(1, System.Math.Max(0, 1.5f - System.Math.Abs(local - 0.5f))));

                    // Draw min/max/average lines
                    local = y - minY;
                    alpha += 0.3f * System.Math.Max(0, 1.8f - 2 * System.Math.Abs(local - 0.5f));
                    if (!float.IsInfinity(_max))
                    {
                        local = y - maxY;
                        alpha += 0.3f * System.Math.Max(0, 1.8f - 2 * System.Math.Abs(local - 0.5f));
                    }
                    if (!float.IsInfinity(_average))
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
            r.Y = Padding * 2 + (string.IsNullOrWhiteSpace(Title) ? 0 : VerticalCaptionSize + Padding);
            r.Width = Bounds.Width - Padding * 2;
            r.Height = Bounds.Height - r.Y - VerticalCaptionSize * 2 - Padding * 4;
            r.X += Bounds.X;
            r.Y += Bounds.Y;
            return r;
        }

        /// <summary>
        /// Process data to build actual data points to draw the graph through,
        /// in relative values.
        /// </summary>
        /// <returns></returns>
        private void BuildCurves()
        {
            // Clear previous results.
            _points.Clear();

            // Skip if there is no data.
            if (_enumerator == null)
            {
                _min = _max = _average = _now = 0;
                return;
            }

            // Initialize values to impossible values to override them.
            _min = float.PositiveInfinity;
            _max = float.NegativeInfinity;
            _now = float.NaN;

            // And start the average at zero, of course.
            _average = 0;
            var count = 0;

            // Start walking over our data source.
            var processed = 0;

            _enumerator.Reset();
            while (_enumerator.MoveNext())
            {
                var data = _enumerator.Current;

                // Build average based on smoothing parameter.
                _movingAverage.Put(data);
                var value = (float)_movingAverage.Mean();

                if (++processed > Smoothing)
                {
                    // Store value in our data point list.
                    _points.Add(value);

                    // Update our extrema.
                    if (value < _min)
                    {
                        _min = value;
                    }
                    if (value > _max)
                    {
                        _max = value;
                    }

                    _average += value;
                    ++count;

                    _now = value;
                }
            }

            // Adjust maximum.
            if (FixedMaximum.HasValue)
            {
                _recentMax = FixedMaximum.Value;
            }
            else if (!float.IsInfinity(_max))
            {
                if (_max > _recentMax)
                {
                    _recentMax = MathHelper.Lerp(_recentMax, _max, 0.1f);
                }
                else
                {
                    _recentMax = MathHelper.Lerp(_recentMax, _max, 0.01f);
                }
            }

            // Scale curve to fit in the graphing area.
            for (int i = 0, j = _points.Count; i < j; i++)
            {
                _points[i] /= _recentMax;
            }

            // Adjust average.
            _average /= count;
        }

        /// <summary>
        /// Formats a value based on the set unit settings for rendering.
        /// </summary>
        /// <param name="caption">The caption to prepend the formatted value.</param>
        /// <param name="value">The value to format.</param>
        /// <returns>
        /// The formatted string representing the value.
        /// </returns>
        private StringBuilder Format(string caption, float value)
        {
            // Reset our formatter.
            _formatter.Clear();

            // Add prefix.
            _formatter.Append(caption);

            // Not much to do if it's infinity.
            if (float.IsInfinity(value) || float.IsNaN(value))
            {
                _formatter.Append("N/A");
                return _formatter;
            }
            
            // See how to process values.
            string[] prefixes;
            float divisor;
            switch (UnitPrefix)
            {
                case UnitPrefixes.SI:
                {
                    prefixes = SIPrefixes;
                    divisor = 1000;
                    break;
                }
                case UnitPrefixes.IEC:
                {
                    prefixes = IECPrefixes;
                    divisor = 1024;
                    break;
                }
                default:
                {
                    // Return default formatted string without unit prefix.
                    AppendFloat(value);
                    _formatter.Append(Unit);
                    return _formatter;
                }
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
            AppendFloat(value);
            _formatter.Append(prefixes[prefix]);
            _formatter.Append(Unit);
            return _formatter;
        }

        private void AppendFloat(float value)
        {
            var preDecimal = (int)value;
            var postDecimal = (int)((value - preDecimal) * 10);

            if (preDecimal < 0)
            {
                _formatter.Append('-');
            }
            AppendInt(preDecimal);
            _formatter.Append('.');
            AppendInt(postDecimal);
            _formatter.Append(' ');
        }

        private void AppendInt(int value)
        {
            var length = 1;
            var tmp = value;
            while (tmp > 10)
            {
                length++;
                tmp /= 10;
            }

            var pos = _formatter.Length;
            _formatter.Append('0', length);
            for (var i = length - 1; i >= 0; --i)
            {
                _formatter[pos + i] = Digits[value % 10];
                value /= 10;
            }
        }

        #endregion
    }
}
