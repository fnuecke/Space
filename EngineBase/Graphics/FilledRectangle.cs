using Microsoft.Xna.Framework;

namespace Engine.Graphics
{
    /// <summary>
    /// Utility class for rendering filled rectangles.
    /// </summary>
    public sealed class FilledRectangle : AbstractShape
    {
        #region Fields
        
        /// <summary>
        /// The current border gradient of the rectangle.
        /// </summary>
        private float _gradient;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new rectangle renderer for the given game.
        /// </summary>
        /// <param name="game"></param>
        public FilledRectangle(Game game)
            : base(game, "Shaders/FilledRectangle")
        {
            // Set defaults.
            SetGradient(1f);
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets a new gradient for this rectangle.
        /// </summary>
        /// <param name="gradient">The new gradient.</param>
        public void SetGradient(float gradient)
        {
            if (gradient != _gradient)
            {
                _gradient = System.Math.Max(1, gradient);
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

            _effect.Parameters["Gradient"].SetValue((_gradient + _gradient) / _width);
        }

        #endregion
    }
}
