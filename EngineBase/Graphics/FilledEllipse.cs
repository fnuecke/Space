using Microsoft.Xna.Framework;

namespace Engine.Graphics
{
    /// <summary>
    /// Utility class for rendering filled ellipses or circles.
    /// </summary>
    public sealed class FilledEllipse : AbstractEllipse
    {
        #region Fields
        
        /// <summary>
        /// The current border gradient of the ellipse.
        /// </summary>
        private float _gradient;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new ellipse renderer for the given game.
        /// </summary>
        /// <param name="game"></param>
        public FilledEllipse(Game game)
            : base(game, "Effects/FilledCircle")
        {
            // Set defaults.
            SetGradient(1f);
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets a new gradient for this ellipse.
        /// </summary>
        /// <param name="gradient">The new gradient.</param>
        public void SetGradient(float gradient)
        {
            if (gradient != _gradient)
            {
                _gradient = gradient;
                _verticesAreValid = false;
            }
        }

        #endregion

        #region Utility stuff

        protected override void AdjustParameters()
        {
            _effect.Parameters["Gradient"].SetValue(_gradient / _majorRadius);
        }

        #endregion
    }
}
