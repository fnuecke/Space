using Microsoft.Xna.Framework;

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
        /// <param name="game">The game we will render for.</param>
        public Rectangle(Game game)
            : base(game, "Shaders/Rectangle")
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
            _vertices[0].Position.X -= _thickness / 2;
            _vertices[0].Position.Y += _thickness / 2;
            // Top right.
            _vertices[1].Position.X += _thickness / 2;
            _vertices[1].Position.Y += _thickness / 2;
            // Bottom left.
            _vertices[2].Position.X -= _thickness / 2;
            _vertices[2].Position.Y -= _thickness / 2;
            // Bottom right.
            _vertices[3].Position.X += _thickness / 2;
            _vertices[3].Position.Y -= _thickness / 2;
        }

        #endregion
    }
}
