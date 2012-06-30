using Microsoft.Xna.Framework;

namespace Engine.Graphics
{
    /// <summary>
    /// Utility class for rendering filled ellipses or circles.
    /// </summary>
    public sealed class FilledEllipse : AbstractEllipse
    {
        #region Properties

        /// <summary>
        /// The gradient for this ellipse.
        /// </summary>
        public float Gradient
        {
            get { return _gradient; }
            set
            {
                if (value != _gradient)
                {
                    _gradient = System.Math.Max(1, value);
                    InvalidateVertices();
                }
            }
        }

        #endregion

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
        /// <param name="game">The game we will render for.</param>
        public FilledEllipse(Game game)
            : base(game, "Shaders/FilledCircle")
        {
            // Set defaults.
            Gradient = 1f;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected override void AdjustParameters()
        {
            base.AdjustParameters();

            Effect.Parameters["Gradient"].SetValue((_gradient + _gradient) / Width);
        }

        #endregion
    }
}
