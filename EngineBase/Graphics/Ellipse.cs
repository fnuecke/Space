using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Graphics
{
    /// <summary>
    /// Utility class for rendering ellipses or circles.
    /// </summary>
    public sealed class Ellipse
    {
        /// <summary>
        /// The quad we draw our ellipse on (i.e. our two triangles).
        /// The complete quad looks like this, with the numbered corners:
        /// <code>
        /// 0 -- 1
        /// |    |
        /// 2 -- 3
        /// </code>
        /// Meaning we want two triangles, the one from 0->1->2, and the
        /// one from 2->1->3 (or anything equivalent).
        /// </summary>
        private static readonly short[] indices = { 0, 1, 2,   // First triangle.
                                                    2, 1, 3 }; // Second triangle.

        /// <summary>
        /// Actual value for our vertex declaration.
        /// </summary>
        private static readonly VertexDeclaration _vertexDeclaration = new VertexDeclaration(new[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            });

        #region Fields
        
        /// <summary>
        /// The shader we use to draw the ellipse.
        /// </summary>
        private static Effect _effect;

        /// <summary>
        /// The graphics device we'll be drawing on.
        /// </summary>
        private GraphicsDevice _device;

        /// <summary>
        /// The list of vertices making up our quad.
        /// </summary>
        private QuadVertex[] _vertices = new QuadVertex[4];

        /// <summary>
        /// Whether our vertices are valid, i.e. correspond to the set ellipse
        /// parameters.
        /// </summary>
        private bool _verticesAreValid;

        /// <summary>
        /// The current center of the ellipse.
        /// </summary>
        private Vector2 _center;

        /// <summary>
        /// The current major radius of the ellipse.
        /// </summary>
        private float _majorRadius;

        /// <summary>
        /// The current minor radius of the ellipse.
        /// </summary>
        private float _minorRadius;

        /// <summary>
        /// The current rotation of the ellipse.
        /// </summary>
        private float _rotation;

        /// <summary>
        /// The current thickness of the ellipse.
        /// </summary>
        private float _thickness;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new ellipse renderer for the given game.
        /// </summary>
        /// <param name="game"></param>
        public Ellipse(Game game)
        {
            if (_effect == null)
            {
                _effect = game.Content.Load<Effect>("Effects/Circle");
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
            SetThickness(1f);
            SetColor(Color.White);
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets a new center for this ellipse.
        /// </summary>
        /// <param name="center">The new center.</param>
        public void SetCenter(ref Vector2 center)
        {
            if (center.X != _center.X || center.Y != _center.Y)
            {
                _center.X = center.X;
                _center.Y = center.Y;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new center for this ellipse.
        /// </summary>
        /// <param name="center">The new center.</param>
        public void SetCenter(Vector2 center)
        {
            SetCenter(ref center);
        }

        /// <summary>
        /// Sets a new major and minor radius for this ellipse, i.e. will make
        /// this a circle with the given radius.
        /// </summary>
        /// <param name="radius">The new radius.</param>
        public void SetRadius(float radius)
        {
            SetMajorRadius(radius);
            SetMinorRadius(radius);
        }

        /// <summary>
        /// Sets a new major radius for this ellipse.
        /// </summary>
        /// <param name="radius">The new major radius.</param>
        public void SetMajorRadius(float radius)
        {
            if (radius != _majorRadius)
            {
                _majorRadius = radius;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new minor radius for this ellipse.
        /// </summary>
        /// <param name="radius">The new minor radius.</param>
        public void SetMinorRadius(float radius)
        {
            if (radius != _minorRadius)
            {
                _minorRadius = radius;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new rotation for this ellipse.
        /// </summary>
        /// <param name="radius">The new rotation.</param>
        public void SetRotation(float angle)
        {
            if (angle != _rotation)
            {
                _rotation = angle;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new rotation for this ellipse.
        /// </summary>
        /// <param name="radius">The new rotation.</param>
        public void SetThickness(float thickness)
        {
            if (thickness != _thickness)
            {
                _thickness = thickness;
                _verticesAreValid = false;
            }
        }

        /// <summary>
        /// Sets a new rotation for this ellipse.
        /// </summary>
        /// <param name="radius">The new rotation.</param>
        public void SetColor(Color color)
        {
            _effect.Parameters["Color"].SetValue(color.ToVector4());
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw the ellipse.
        /// </summary>
        public void Draw()
        {
            if (!_verticesAreValid)
            {
                RecomputeQuads();
            }

            _effect.CurrentTechnique.Passes[0].Apply();
            _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, 4, indices, 0, 2, _vertexDeclaration);
        }

        #endregion

        #region Utility stuff

        /// <summary>
        /// Utility method to recompute position of quads if a ellipse
        /// parameter was changed.
        /// </summary>
        private void RecomputeQuads()
        {
            // Reset corner positions.

            // Top left.
            _vertices[0].Position.X = -_majorRadius - _thickness / 2;
            _vertices[0].Position.Y = _minorRadius + _thickness / 2;
            _vertices[0].Position.Z = 0;
            // Top right.
            _vertices[1].Position.X = _majorRadius + _thickness / 2;
            _vertices[1].Position.Y = _minorRadius + _thickness / 2;
            _vertices[1].Position.Z = 0;
            // Bottom left.
            _vertices[2].Position.X = -_majorRadius - _thickness / 2;
            _vertices[2].Position.Y = -_minorRadius - _thickness / 2;
            _vertices[2].Position.Z = 0;
            // Bottom right.
            _vertices[3].Position.X = _majorRadius + _thickness / 2;
            _vertices[3].Position.Y = -_minorRadius - _thickness / 2;
            _vertices[3].Position.Z = 0;

            // Build transforms.
            Matrix transform =
                // Rotate as specified, around the origin.
                Matrix.CreateRotationZ(-_rotation)
                // Position to the specified center. Make our coordinate system
                // start at the top left, so subtract half the screen width,
                // and invert the y axis (also subtract there).
                * Matrix.CreateTranslation(_center.X - _device.Viewport.Width / 2f, _device.Viewport.Height / 2f - _center.Y, 0)
                // Finally map what we have to screen space.
                * Matrix.CreateOrthographic(_device.Viewport.Width, _device.Viewport.Height, _device.Viewport.MinDepth, _device.Viewport.MaxDepth);
            // Apply transform to each corner.
            Vector3.Transform(ref _vertices[0].Position, ref transform, out _vertices[0].Position);
            Vector3.Transform(ref _vertices[1].Position, ref transform, out _vertices[1].Position);
            Vector3.Transform(ref _vertices[2].Position, ref transform, out _vertices[2].Position);
            Vector3.Transform(ref _vertices[3].Position, ref transform, out _vertices[3].Position);

            // Adjust line thickness.
            _effect.Parameters["Thickness"].SetValue(_thickness / _majorRadius);

            _verticesAreValid = true;
        }

        /// <summary>
        /// Represents one corner of a quad into which we will draw an ellipse.
        /// </summary>
        private struct QuadVertex
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
