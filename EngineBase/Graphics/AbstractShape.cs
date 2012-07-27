using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
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
        protected static readonly short[] Indices = { 0, 1, 2,   // First triangle.
                                                      2, 1, 3 }; // Second triangle.

        /// <summary>
        /// Actual value for our vertex declaration.
        /// </summary>
        protected static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            });

        #endregion

        #region Properties

        /// <summary>
        /// The graphics device to which this shape will be rendered.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get { return Device; } }

        /// <summary>
        /// The center for this shape.
        /// </summary>
        public Vector2 Center
        {
            get { return _center; }
            set { SetCenter(value.X, value.Y); }
        }

        /// <summary>
        /// The scaling for this shape.
        /// </summary>
        public float Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                InvalidateVertices();
            }
        }

        /// <summary>
        /// The width for this shape.
        /// </summary>
        public float Width
        {
            get { return _width; }
            set
            {
                _width = value;
                InvalidateVertices();
            }
        }

        /// <summary>
        /// The height for this shape.
        /// </summary>
        public float Height
        {
            get { return _height; }
            set
            {
                _height = value;
                InvalidateVertices();
            }
        }

        /// <summary>
        /// The rotation for this shape.
        /// </summary>
        public float Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                InvalidateVertices();
            }
        }

        /// <summary>
        /// The color for this shape.
        /// </summary>
        public Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                InvalidateVertices();
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The graphics device we'll be drawing on.
        /// </summary>
        protected readonly GraphicsDevice Device;

        /// <summary>
        /// The shader we use to draw the ellipse.
        /// </summary>
        protected readonly Effect Effect;

        /// <summary>
        /// The list of vertices making up our quad.
        /// </summary>
        protected readonly QuadVertex[] Vertices = new QuadVertex[4];

        /// <summary>
        /// Whether our vertices are valid, i.e. correspond to the set shape
        /// parameters.
        /// </summary>
        private bool _verticesAreValid;

        /// <summary>
        /// The current center of the shape.
        /// </summary>
        private Vector2 _center;

        /// <summary>
        /// The current width of the shape.
        /// </summary>
        private float _width;

        /// <summary>
        /// The current height of the shape.
        /// </summary>
        private float _height;

        /// <summary>
        /// The current rotation of the shape.
        /// </summary>
        private float _rotation;

        /// <summary>
        /// The color of the shape.
        /// </summary>
        private Color _color = Color.White;

        /// <summary>
        /// The scale of the shape
        /// </summary>
        private float _scale = 1.0f;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new ellipse renderer for the given game.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics device to render to.</param>
        /// <param name="effectName">The shader to use for rendering the shape.</param>
        protected AbstractShape(ContentManager content, GraphicsDevice graphics, string effectName)
        {
            if (Effect == null)
            {
                Effect = content.Load<Effect>(effectName);
            }
            Device = graphics;

            // Set texture coordinates.
            Vertices[0].Tex0.X = -1;
            Vertices[0].Tex0.Y = -1;
            Vertices[1].Tex0.X = 1;
            Vertices[1].Tex0.Y = -1;
            Vertices[2].Tex0.X = -1;
            Vertices[2].Tex0.Y = 1;
            Vertices[3].Tex0.X = 1;
            Vertices[3].Tex0.Y = 1;
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
            _center.X = x;
            _center.Y = y;
            InvalidateVertices();
        }

        /// <summary>
        /// Sets a new size for this shape.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public void SetSize(float width, float height)
        {
            Width = width;
            Height = height;
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
        /// Marks the vertices as invalid, so that they are recomputed before the next draw.
        /// </summary>
        protected void InvalidateVertices()
        {
            _verticesAreValid = false;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw the shape.
        /// </summary>
        public virtual void Draw()
        {
            // Update our paint canvas if necessary.
            RecomputeQuads();

            // Always adjust shader parameters, because it may be re-used by
            // different shape renderers.
            AdjustParameters();

            // Apply the effect and render our area.
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, 4, Indices, 0, 2, VertexDeclaration);
            }
        }

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected virtual void AdjustParameters()
        {
            var color = Effect.Parameters["Color"];
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
            // Skip if all is in order.
            if (_verticesAreValid)
            {
                return;
            }

            // Adjust bounds.
            AdjustBounds();

            // Build transforms.
            var transform =
                // Rotate as specified, around the origin.
                Matrix.CreateRotationZ(-_rotation)
                // Position to the specified center. Make our coordinate system
                // start at the top left, so subtract half the screen width,
                // and invert the y axis (also subtract there).
                * Matrix.CreateTranslation(_center.X - Device.Viewport.Width / 2f, Device.Viewport.Height / 2f - _center.Y, 0)
                // Apply scaling to the object (at this point to scale relative
                // to its center)
                * Matrix.CreateScale(_scale)
                // Finally map what we have to screen space.
                * Matrix.CreateOrthographic(Device.Viewport.Width, Device.Viewport.Height, Device.Viewport.MinDepth, Device.Viewport.MaxDepth);
            // Apply transform to each corner.
            Vector3.Transform(ref Vertices[0].Position, ref transform, out Vertices[0].Position);
            Vector3.Transform(ref Vertices[1].Position, ref transform, out Vertices[1].Position);
            Vector3.Transform(ref Vertices[2].Position, ref transform, out Vertices[2].Position);
            Vector3.Transform(ref Vertices[3].Position, ref transform, out Vertices[3].Position);

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
            Vertices[0].Position.X = -_width / 2 - 0.5f;
            Vertices[0].Position.Y = _height / 2 + 0.5f;
            Vertices[0].Position.Z = 0;
            // Top right.
            Vertices[1].Position.X = _width / 2 + 0.5f;
            Vertices[1].Position.Y = _height / 2 + 0.5f;
            Vertices[1].Position.Z = 0;
            // Bottom left.
            Vertices[2].Position.X = -_width / 2 - 0.5f;
            Vertices[2].Position.Y = -_height / 2 - 0.5f;
            Vertices[2].Position.Z = 0;
            // Bottom right.
            Vertices[3].Position.X = _width / 2 + 0.5f;
            Vertices[3].Position.Y = -_height / 2 - 0.5f;
            Vertices[3].Position.Z = 0;
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
                Position = xyz;
                Tex0 = uv;
            }

            #endregion
        }

        #endregion
    }
}
