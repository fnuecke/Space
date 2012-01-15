using Microsoft.Xna.Framework;

namespace Engine.Graphics
{
    /// <summary>
    /// Utility class for rendering ellipses or circles.
    /// </summary>
    public sealed class Ellipse : AbstractEllipse
    {
        #region Fields
        
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
            : base(game, "Effects/Circle")
        {
            // Set defaults.
            SetThickness(1f);
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets a new thickness for this ellipse.
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

        #region Utility stuff

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

        protected override void AdjustParameters()
        {
            _effect.Parameters["Thickness"].SetValue(_thickness / _majorRadius);
        }

        #endregion
    }
}
