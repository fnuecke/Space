using Microsoft.Xna.Framework;

namespace Engine.Graphics
{
    /// <summary>
    /// Utility class for rendering rectangles.
    /// </summary>
    public sealed class Rectangle : AbstractShape
    {
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
        /// <param name="game"></param>
        public Rectangle(Game game)
            : base(game, "Effects/Rectangle")
        {
            // Set defaults.
            SetThickness(1f);
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets a new thickness for this rectangle.
        /// </summary>
        /// <param name="thickness">The new thickness.</param>
        public void SetThickness(float thickness)
        {
            if (thickness != _thickness)
            {
                _thickness = thickness;
                _verticesAreValid = false;
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected override void AdjustParameters()
        {
            base.AdjustParameters();

            _effect.Parameters["Thickness"].SetValue((_thickness + _thickness) / _width);
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
