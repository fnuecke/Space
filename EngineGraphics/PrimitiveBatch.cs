using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Graphics
{
    /// <summary>
    /// This is a utility class alike XNA's own <see cref="SpriteBatch"/> class
    /// that may be used to render to the graphics device. This class allows
    /// rendering basic primitives such as lines, circles and general polygons.
    /// </summary>
    /// <remarks>
    /// The basic implementation was taken from FarSeer Physics but adjusted
    /// quite a bit.
    /// </remarks>
    public sealed class PrimitiveBatch : IDisposable
    {
        #region Constants

        /// <summary>
        /// The default buffer size, i.e. the number of vertices we buffer before
        /// submitting a draw request to the graphics card.
        /// </summary>
        private const uint DefaultBufferSize = 0x2000;

        /// <summary>
        /// The number of segments we use when rendering circles. Higher number
        /// means smoother circles, but more expensive draw operations.
        /// </summary>
        private const int CircleSegments = 32;

        #endregion

        #region Fields

        /// <summary>
        /// The effect (shader) we use for drawing our primitives.
        /// </summary>
        private readonly BasicEffect _basicEffect;

        /// <summary>
        /// The device that we will issue draw calls to.
        /// </summary>
        private readonly GraphicsDevice _device;

        /// <summary>
        /// Tracks whether we've already disposed our resources.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// We use this flag to track whether we're currently inside a batch call,
        /// to make sure users don't call <see cref="End"/> before <see cref="Begin(Microsoft.Xna.Framework.Matrix)"/>
        /// is called, and don't call <see cref="Begin(Microsoft.Xna.Framework.Matrix)"/> again befure <see cref="End"/>
        /// is called.
        /// </summary>
        private bool _inBeginEndPair;

        /// <summary>
        /// The transform matrix we use for the current batch.
        /// </summary>
        private Matrix _transform;

        /// <summary>
        /// Our line vertex buffer. This stores line draw operations by
        /// saving the vertices of the draw operations in groups of two.
        /// </summary>
        private readonly VertexPositionColor[] _lineVertices;

        /// <summary>
        /// The number of actually buffered line vertices.
        /// </summary>
        private int _lineVertexCount;

        /// <summary>
        /// Our triangle vertex buffer. This stores triangle draw operations
        /// by saving the vertices of the draw operations in groups of three.
        /// </summary>
        private readonly VertexPositionColor[] _triangleVertices;

        /// <summary>
        /// The number of actually buffered triangle vertices.
        /// </summary>
        private int _triangleVertexCount;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveBatch"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to render to.</param>
        /// <param name="bufferSize">Size of the vertex buffers (batches).</param>
        public PrimitiveBatch(GraphicsDevice graphicsDevice, uint bufferSize = DefaultBufferSize)
        {
            // Make sure we were given a valid graphics device.
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("graphicsDevice");
            }
            _device = graphicsDevice;

            // Allocate our buffers. Make sure they are at least as large as the specified
            // buffer size, but also have a length that is a multiple of the vertices used
            // for rendering the shape for which we use them (3 for triangles, 2 for line).
            _triangleVertices = new VertexPositionColor[bufferSize - bufferSize % 3 + 3];
            _lineVertices = new VertexPositionColor[bufferSize - bufferSize % 2 + 2];

            System.Diagnostics.Debug.Assert(_triangleVertices.Length % 3 == 0);
            System.Diagnostics.Debug.Assert(_lineVertices.Length % 2 == 0);

            // Set up a new basic effect, and enable vertex colors, which is the main
            // means of drawing our primitives.
            _basicEffect = new BasicEffect(graphicsDevice) {VertexColorEnabled = true};
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="PrimitiveBatch"/> is reclaimed by garbage collection.
        /// </summary>
        ~PrimitiveBatch()
        {
            _basicEffect.Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged
        /// resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                if (_basicEffect != null)
                {
                    _basicEffect.Dispose();
                }

                _isDisposed = true;
            }
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Begin is called to initialize the graphics card for our draw.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix to apply when drawing
        /// this batch.</param>
        public void Begin(Matrix transformMatrix)
        {
            if (_inBeginEndPair)
            {
                throw new InvalidOperationException("End must be called before Begin can be called again.");
            }

            _transform = transformMatrix;

            // flip the error checking boolean. It's now ok to call AddVertex, Flush,
            // and End.
            _inBeginEndPair = true;
        }

        /// <summary>
        /// Begin is called to initialize the graphics card for our draw.
        /// </summary>
        public void Begin()
        {
            Begin(Matrix.Identity);
        }

        /// <summary>
        /// End is called once all the primitives have been drawn using AddVertex.
        /// it will call Flush to actually submit the draw call to the graphics card, and
        /// then tell the basic effect to end.
        /// </summary>
        public void End()
        {
            if (!_inBeginEndPair)
            {
                throw new InvalidOperationException("Begin must be called before End can be called.");
            }

            // Draw whatever the user wanted us to draw
            FlushTriangles();
            FlushLines();

            _inBeginEndPair = false;
        }

        /// <summary>
        /// Draws a line from the specified start point to the specified end point.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <param name="color">The color of the line.</param>
        public void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            BufferLineVertex(start, color);
            BufferLineVertex(end, color);
        }

        /// <summary>
        /// Draws the polygon outline of the polygon define by the specified vertices.
        /// </summary>
        /// <param name="vertices">The vertices of the polygon.</param>
        /// <param name="color">The outline color.</param>
        public void DrawPolygon(IList<Vector2> vertices, Color color)
        {
            for (var i = 0; i < vertices.Count - 1; i++)
            {
                BufferLineVertex(vertices[i], color);
                BufferLineVertex(vertices[i + 1], color);
            }

            BufferLineVertex(vertices[vertices.Count - 1], color);
            BufferLineVertex(vertices[0], color);
        }

        /// <summary>
        /// Draws the filled polygon of the polygon define by the specified vertices.
        /// If <paramref name="outline"/> is <c>true</c> the fill color will be half
        /// transparent and the border of the specified <paramref name="color"/>.
        /// </summary>
        /// <param name="vertices">The vertices of the polygon.</param>
        /// <param name="color">The fill/outline color.</param>
        /// <param name="outline">if set to <c>true</c> will also draw an outline.</param>
        public void DrawFilledPolygon(IList<Vector2> vertices, Color color, bool outline = true)
        {
            // Is it actually a line?
            if (vertices.Count == 2)
            {
                DrawLine(vertices[0], vertices[1], color);
                return;
            }

            // See how we want to fill.
            var fillColor = color * (outline ? 0.5f : 1.0f);
            for (var i = 1; i < vertices.Count - 1; i++)
            {
                BufferTriangleVertex(vertices[0], fillColor);
                BufferTriangleVertex(vertices[i], fillColor);
                BufferTriangleVertex(vertices[i + 1], fillColor);
            }

            // Draw outline if requested.
            if (outline)
            {
                DrawPolygon(vertices, color);
            }
        }

        /// <summary>
        /// Draws a box outline centered at the specified position, with the specified size.
        /// </summary>
        /// <param name="position">The center position of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="color">The fill/outline color.</param>
        public void DrawRectangle(Vector2 position, float width, float height, Color color)
        {
            var halfWidth = width / 2.0f;
            var halfHeight = height / 2.0f;
            var vertices = new[]
            {
                position + new Vector2(-halfWidth, -halfHeight),
                position + new Vector2(halfWidth, -halfHeight),
                position + new Vector2(halfWidth, halfHeight),
                position + new Vector2(-halfWidth, halfHeight)
            };

            DrawPolygon(vertices, color);
        }

        /// <summary>
        /// Draws a filled box centered at the specified position, with the specified size.
        /// If <paramref name="outline"/> is <c>true</c> the fill color will be half
        /// transparent and the border of the specified <paramref name="color"/>.
        /// </summary>
        /// <param name="position">The center position of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="color">The fill/outline color.</param>
        /// <param name="outline">if set to <c>true</c> will also draw an outline.</param>
        public void DrawFilledRectangle(Vector2 position, float width, float height, Color color, bool outline = true)
        {
            var halfWidth = width / 2.0f;
            var halfHeight = height / 2.0f;
            var vertices = new[]
            {
                position + new Vector2(-halfWidth, -halfHeight),
                position + new Vector2(halfWidth, -halfHeight),
                position + new Vector2(halfWidth, halfHeight),
                position + new Vector2(-halfWidth, halfHeight)
            };

            DrawFilledPolygon(vertices, color, outline);
        }

        /// <summary>
        /// Draws the circle outline with the specified properties.
        /// </summary>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the cirle.</param>
        /// <param name="color">The outline color.</param>
        public void DrawCircle(Vector2 center, float radius, Color color)
        {
            const double increment = System.Math.PI * 2.0 / CircleSegments;
            var theta = 0.0;

            for (var i = 0; i < CircleSegments; i++)
            {
                var v1 = center + radius * new Vector2((float)System.Math.Cos(theta), (float)System.Math.Sin(theta));
                var v2 = center +
                             radius *
                             new Vector2((float)System.Math.Cos(theta + increment), (float)System.Math.Sin(theta + increment));

                BufferLineVertex(v1, color);
                BufferLineVertex(v2, color);

                theta += increment;
            }
        }

        /// <summary>
        /// Draws the filled circle with the specified properties.
        /// </summary>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the cirle.</param>
        /// <param name="color">The outline color.</param>
        /// <param name="axis">if set to <c>true</c> renders a rotation indicator along the x axis.</param>
        public void DrawSolidCircle(Vector2 center, float radius, Color color, bool axis = true)
        {
            const double increment = System.Math.PI * 2.0 / CircleSegments;
            var theta = 0.0;

            var colorFill = color * 0.5f;

            var v0 = center + radius * new Vector2((float)System.Math.Cos(theta), (float)System.Math.Sin(theta));
            theta += increment;

            for (var i = 1; i < CircleSegments - 1; i++)
            {
                var v1 = center + radius * new Vector2((float)System.Math.Cos(theta), (float)System.Math.Sin(theta));
                var v2 = center + radius * new Vector2((float)System.Math.Cos(theta + increment), (float)System.Math.Sin(theta + increment));

                BufferTriangleVertex(v0, colorFill);
                BufferTriangleVertex(v1, colorFill);
                BufferTriangleVertex(v2, colorFill);

                theta += increment;
            }
            DrawCircle(center, radius, color);

            if (axis)
            {
                DrawLine(center, center + Vector2.UnitX * radius, color);
            }
        }

        public void DrawArrow(Vector2 start, Vector2 end, float length, float width, bool drawStartIndicator,
                              Color color)
        {
            // Draw connection segment between start- and end-point
            DrawLine(start, end, color);

            // Precalculate halfwidth
            float halfWidth = width / 2;

            // Create directional reference
            Vector2 rotation = (start - end);
            rotation.Normalize();

            // Calculate angle of directional vector
            float angle = (float)System.Math.Atan2(rotation.X, -rotation.Y);
            // Create matrix for rotation
            Matrix rotMatrix = Matrix.CreateRotationZ(angle);
            // Create translation matrix for end-point
            Matrix endMatrix = Matrix.CreateTranslation(end.X, end.Y, 0);

            // Setup arrow end shape
            var verts = new Vector2[3];
            verts[0] = new Vector2(0, 0);
            verts[1] = new Vector2(-halfWidth, -length);
            verts[2] = new Vector2(halfWidth, -length);

            // Rotate end shape
            Vector2.Transform(verts, ref rotMatrix, verts);
            // Translate end shape
            Vector2.Transform(verts, ref endMatrix, verts);

            // Draw arrow end shape
            DrawFilledPolygon(verts, color, false);

            if (drawStartIndicator)
            {
                // Create translation matrix for start
                var startMatrix = Matrix.CreateTranslation(start.X, start.Y, 0);
                // Setup arrow start shape
                var baseVerts = new Vector2[4];
                baseVerts[0] = new Vector2(-halfWidth, length / 4);
                baseVerts[1] = new Vector2(halfWidth, length / 4);
                baseVerts[2] = new Vector2(halfWidth, 0);
                baseVerts[3] = new Vector2(-halfWidth, 0);

                // Rotate start shape
                Vector2.Transform(baseVerts, ref rotMatrix, baseVerts);
                // Translate start shape
                Vector2.Transform(baseVerts, ref startMatrix, baseVerts);
                // Draw start shape
                DrawFilledPolygon(baseVerts, color, false);
            }
        }

        #endregion

        #region Buffering

        /// <summary>
        /// Buffers the specified line vertex. If the buffer is full this will
        /// flush the buffer.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="color">The color.</param>
        private void BufferTriangleVertex(Vector2 vertex, Color color)
        {
            if (!_inBeginEndPair)
            {
                throw new InvalidOperationException("Begin must be called before drawing.");
            }

            // Flush if the buffer is full.
            if (_triangleVertexCount >= _triangleVertices.Length)
            {
                FlushTriangles();
            }

            // Push the vertex.
            // TODO offset in z direction was -0.1f in FarSeer, necessary?
            _triangleVertices[_triangleVertexCount].Position = new Vector3(vertex, 0);
            _triangleVertices[_triangleVertexCount].Color = color;
            ++_triangleVertexCount;
        }

        /// <summary>
        /// Buffers the specified line vertex. If the buffer is full this will
        /// flush the buffer.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="color">The color.</param>
        private void BufferLineVertex(Vector2 vertex, Color color)
        {
            if (!_inBeginEndPair)
            {
                throw new InvalidOperationException("Begin must be called before drawing.");
            }

            // Flush if the buffer is full.
            if (_lineVertexCount >= _lineVertices.Length)
            {
                FlushLines();
            }

            // Push the vertex.
            _lineVertices[_lineVertexCount].Position = new Vector3(vertex, 0);
            _lineVertices[_lineVertexCount].Color = color;
            ++_lineVertexCount;
        }

        /// <summary>
        /// Flushes all buffered triangle draw operations to the graphics card.
        /// </summary>
        private void FlushTriangles()
        {
            System.Diagnostics.Debug.Assert(_inBeginEndPair);
            System.Diagnostics.Debug.Assert(_triangleVertexCount >= 0 && _triangleVertexCount % 3 == 0);

            // Do we have enough data to draw anything at all?
            if (_triangleVertexCount == 0)
            {
                return;
            }

            // TODO adjust matrix to make sure this isn't necessary.
            // Invert vertex order.
            Array.Reverse(_triangleVertices, 0, _triangleVertexCount);

            // Submit the draw call to the graphics card.
            SetRenderState();
            _device.DrawUserPrimitives(PrimitiveType.TriangleList, _triangleVertices, 0, _triangleVertexCount / 3);

            // Reset our buffer.
            _triangleVertexCount = 0;
        }

        /// <summary>
        /// Flushes all buffered line draw operations to the graphics card.
        /// </summary>
        private void FlushLines()
        {
            System.Diagnostics.Debug.Assert(_inBeginEndPair);
            System.Diagnostics.Debug.Assert(_lineVertexCount >= 0 && _lineVertexCount % 2 == 0);

            // Do we have enough data to draw anything at all?
            if (_lineVertexCount == 0)
            {
                return;
            }

            // Submit the draw call to the graphics card.
            SetRenderState();
            _device.DrawUserPrimitives(PrimitiveType.LineList, _lineVertices, 0, _lineVertexCount / 2);

            // Reset our buffer.
            _lineVertexCount = 0;
        }

        /// <summary>
        /// Sets up our effect for rendering a batch of shapes.
        /// </summary>
        private void SetRenderState()
        {
            _device.BlendState = BlendState.AlphaBlend;
            _device.DepthStencilState = DepthStencilState.None;
            _device.RasterizerState = RasterizerState.CullCounterClockwise;
            _device.SamplerStates[0] = SamplerState.LinearClamp;

            var viewport = _device.Viewport;
            _basicEffect.Projection = Matrix
                .CreateOrthographicOffCenter(0, viewport.Width,
                                             0, viewport.Height,
                                             0, 1);
            _basicEffect.View = _transform;
            _basicEffect.CurrentTechnique.Passes[0].Apply();
        }

        #endregion
    }
}