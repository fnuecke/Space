using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Graphics
{
    public abstract class AbstractShape
    {
        #region Constants

        /// <summary>
        /// The quad we draw our shape on (i.e. our two triangles).
        /// The complete quad looks like this, with the numbered corners:
        /// <code>
        /// 0 -- 1
        /// |    |
        /// 2 -- 3
        /// </code>
        /// Meaning we want two triangles, the one from 0->1->2, and the
        /// one from 2->1->3 (or anything equivalent).
        /// </summary>
        protected static readonly short[] _indices = { 0, 1, 2,   // First triangle.
                                                       2, 1, 3 }; // Second triangle.

        /// <summary>
        /// Actual value for our vertex declaration.
        /// </summary>
        protected static readonly VertexDeclaration _vertexDeclaration = new VertexDeclaration(new[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            });

        #endregion

        #region Properties

        /// <summary>
        /// The graphics device to which this shape will be rendered.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get { return _device; } }

        #endregion

        #region Fields

        /// <summary>
        /// The shader we use to draw the ellipse.
        /// </summary>
        protected Effect _effect;

        /// <summary>
        /// The graphics device we'll be drawing on.
        /// </summary>
        protected GraphicsDevice _device;

        /// <summary>
        /// The list of vertices making up our quad.
        /// </summary>
        protected QuadVertex[] _vertices = new QuadVertex[4];

        /// <summary>
        /// Whether our vertices are valid, i.e. correspond to the set shape
        /// parameters.
        /// </summary>
        protected bool _verticesAreValid;

        /// <summary>
        /// The current center of the shape.
        /// </summary>
        protected Vector2 _center;

        /// <summary>
        /// The current width of the shape.
        /// </summary>
        protected float _width;

        /// <summary>
        /// The current height of the shape.
        /// </summary>
        protected float _height;

        /// <summary>
        /// The current rotation of the shape.
        /// </summary>
        protected float _rotation;

        /// <summary>
        /// The color of the shape.
        /// </summary>
        protected Color _color;

        /// <summary>
        /// The scale of the shape
        /// </summary>
        protected float _scale = 1.0f;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new ellipse renderer for the given game.
        /// </summary>
        /// <param name="game"></param>
        protected AbstractShape(Game game, string effectName)
        {
            if (_effect == null)
            {
                _effect = game.Content.Load<Effect>(effectName);
            }
            _device = game.GraphicsDevice;

            // Set texture coordinates.
            _vertices[0].Tex0.X = -1;
            _vertices[0].Tex0.Y = -1;
            _vertices[1].Tex0.X = 1;
            _vertices[1].Tex0.Y = -1;
            _vertices[2].Tex0.X = -1;
            _vertices[2].Tex0.Y = 1;
            _vertices[3].Tex0.X = 1;
            _vertices[3].Tex0.Y = 1;

            // Set defaults.
            SetColor(Color.White);
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets a new center for this shape.
        /// </summary>
        /// <param name="x">The x coordinate of the new center.</param>
        /// <param name="y">The y coordinate of the new center.</param>
        public void SetCenter(float x, float y)
        {
            if (x != _center.X || y != _center.Y)
            {
                _center.X = x;
                _center.Y = y;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new center for this shape.
        /// </summary>
        /// <param name="center">The new center.</param>
        public void SetCenter(ref Vector2 center)
        {
            SetCenter(center.X, center.Y);
        }

        /// <summary>
        /// Sets a new center for this shape.
        /// </summary>
        /// <param name="center">The new center.</param>
        public void SetCenter(Vector2 center)
        {
            SetCenter(center.X, center.Y);
        }

        /// <summary>
        /// Sets a new scaling for this shape.
        /// </summary>
        /// <param name="size">The new size.</param>
        public void SetScale(float scale)
        {
            if (scale != _scale)
            {
                _scale = scale;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new size for this shape.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public void SetSize(float width, float height)
        {
            SetWidth(width);
            SetHeight(height);
        }

        /// <summary>
        /// Sets a new size for this shape, i.e. will  set width and height to
        /// this value.
        /// </summary>
        /// <param name="size">The new size.</param>
        public void SetSize(float size)
        {
            SetSize(size, size);
        }

        /// <summary>
        /// Sets a new width for this shape.
        /// </summary>
        /// <param name="width">The new width.</param>
        public void SetWidth(float width)
        {
            if (width != _width)
            {
                _width = width;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new height for this shape.
        /// </summary>
        /// <param name="height">The new height.</param>
        public void SetHeight(float height)
        {
            if (height != _height)
            {
                _height = height;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new rotation for this shape.
        /// </summary>
        /// <param name="rotation">The new rotation.</param>
        public void SetRotation(float rotation)
        {
            if (rotation != _rotation)
            {
                _rotation = rotation;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new color for this shape.
        /// </summary>
        /// <param name="color">The new color.</param>
        public void SetColor(Color color)
        {
            if (color != _color)
            {
                _color = color;
                _verticesAreValid = false;
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw the shape.
        /// </summary>
        public virtual void Draw()
        {
            // Update our paint canvas if necessary.
            if (!_verticesAreValid)
            {
                RecomputeQuads();
            }

            // Always adjust shader parameters, because it may be re-used by
            // different shape renderers.
            AdjustParameters();

            // Apply the effect and render our area.
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, 4, _indices, 0, 2, _vertexDeclaration);
            }
        }

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected virtual void AdjustParameters()
        {
            var color = _effect.Parameters["Color"];
            if (color != null)
            {
                color.SetValue(_color.ToVector4());
            }
        }

        #endregion

        #region Utility stuff

        /// <summary>
        /// Utility method to recompute position of quads if a parameter was
        /// changed.
        /// </summary>
        protected void RecomputeQuads()
        {
            // Adjust bounds.
            AdjustBounds();

            // Build transforms.
            Matrix transform =
                // Rotate as specified, around the origin.
                Matrix.CreateRotationZ(-_rotation)
                // Position to the specified center. Make our coordinate system
                // start at the top left, so subtract half the screen width,
                // and invert the y axis (also subtract there).
                * Matrix.CreateTranslation(_center.X - _device.Viewport.Width / 2f, _device.Viewport.Height / 2f - _center.Y, 0)
                // Apply scaling to the object (at this point to scale relative
                // to its center)
                * Matrix.CreateScale(_scale)
                // Finally map what we have to screen space.
                * Matrix.CreateOrthographic(_device.Viewport.Width, _device.Viewport.Height, _device.Viewport.MinDepth, _device.Viewport.MaxDepth);
            // Apply transform to each corner.
            Vector3.Transform(ref _vertices[0].Position, ref transform, out _vertices[0].Position);
            Vector3.Transform(ref _vertices[1].Position, ref transform, out _vertices[1].Position);
            Vector3.Transform(ref _vertices[2].Position, ref transform, out _vertices[2].Position);
            Vector3.Transform(ref _vertices[3].Position, ref transform, out _vertices[3].Position);

            _verticesAreValid = true;
        }

        /// <summary>
        /// Adjusts the bounds of the shape, in the sense that it adjusts the
        /// positions of the vertices' texture coordinates if required for the
        /// effect to work correctly.
        /// </summary>
        protected virtual void AdjustBounds()
        {
            // Reset corner positions.

            // Top left.
            _vertices[0].Position.X = -_width / 2 - 0.5f;
            _vertices[0].Position.Y = _height / 2 + 0.5f;
            _vertices[0].Position.Z = 0;
            // Top right.
            _vertices[1].Position.X = _width / 2 + 0.5f;
            _vertices[1].Position.Y = _height / 2 + 0.5f;
            _vertices[1].Position.Z = 0;
            // Bottom left.
            _vertices[2].Position.X = -_width / 2 - 0.5f;
            _vertices[2].Position.Y = -_height / 2 - 0.5f;
            _vertices[2].Position.Z = 0;
            // Bottom right.
            _vertices[3].Position.X = _width / 2 + 0.5f;
            _vertices[3].Position.Y = -_height / 2 - 0.5f;
            _vertices[3].Position.Z = 0;
        }

        /// <summary>
        /// Represents one corner of a quad into which we will draw an ellipse.
        /// </summary>
        protected struct QuadVertex
        {
            #region Fields

            /// <summary>
            /// The position of the corner, in space.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// The texture coordinate at that vertex.
            /// </summary>
            public Vector2 Tex0;

            #endregion

            #region Constructor

            /// <summary>
            /// Creates a new quad vertex, initialized to the given values.
            /// </summary>
            /// <param name="xyz">The spatial position of the vertex.</param>
            /// <param name="uv">The texture coordinate at the vertex.</param>
            public QuadVertex(Vector3 xyz, Vector2 uv)
            {
                this.Position = xyz;
                this.Tex0 = uv;
            }

            #endregion
        }

        #endregion
    }
}
