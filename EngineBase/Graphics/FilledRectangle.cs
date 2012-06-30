using Microsoft.Xna.Framework;

namespace Engine.Graphics
{
    /// <summary>
    /// Utility class for rendering filled rectangles.
    /// </summary>
    public sealed class FilledRectangle : AbstractShape
    {
        #region Properties

        /// <summary>
        /// The gradient for this rectangle.
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
        /// The current border gradient of the rectangle.
        /// </summary>
        private float _gradient;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new rectangle renderer for the given game.
        /// </summary>
        /// <param name="game">The game we will render for.</param>
        public FilledRectangle(Game game)
            : base(game, "Shaders/FilledRectangle")
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
