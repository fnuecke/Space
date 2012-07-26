using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Graphics
{
    /// <summary>
    /// Utility class for rendering rectangles.
    /// </summary>
    public sealed class Rectangle : AbstractShape
    {
        #region Properties

        /// <summary>
        /// The thickness for this rectangle.
        /// </summary>
        public float Thickness
        {
            get { return _thickness; }
            set
            {
                if (value != _thickness)
                {
                    _thickness = value;
                    InvalidateVertices();
                }
            }
        }

        #endregion

        #region Fields
        
        /// <summary>
        /// The current thickness of the rectangle.
        /// </summary>
        private float _thickness;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new rectangle renderer for the given game.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics device to render to.</param>
        public Rectangle(ContentManager content, GraphicsDevice graphics)
            : base(content, graphics, "Shaders/Rectangle")
        {
            // Set defaults.
            Thickness = 1f;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected override void AdjustParameters()
        {
            base.AdjustParameters();

            Effect.Parameters["Thickness"].SetValue((_thickness + _thickness) / Width);
        }

        #endregion

        #region Utility stuff

        /// <summary>
        /// Adjusts the bounds of the shape, in the sense that it adjusts the
        /// positions of the vertices' texture coordinates if required for the
        /// effect to work correctly.
        /// </summary>
        protected override void AdjustBounds()
        {
            base.AdjustBounds();

            // Top left.
            Vertices[0].Position.X -= _thickness / 2;
            Vertices[0].Position.Y += _thickness / 2;
            // Top right.
            Vertices[1].Position.X += _thickness / 2;
            Vertices[1].Position.Y += _thickness / 2;
            // Bottom left.
            Vertices[2].Position.X -= _thickness / 2;
            Vertices[2].Position.Y -= _thickness / 2;
            // Bottom right.
            Vertices[3].Position.X += _thickness / 2;
            Vertices[3].Position.Y -= _thickness / 2;
        }

        #endregion
    }
}
