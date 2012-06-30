using Microsoft.Xna.Framework;

namespace Engine.Graphics
{
    /// <summary>
    /// Base class for ellipse / circle shape renderers.
    /// </summary>
    public abstract class AbstractEllipse : AbstractShape
    {
        #region Properties

        /// <summary>
        /// The major and minor radius for this ellipse, i.e. will make
        /// this a circle with the given radius.
        /// </summary>
        public float Radius
        {
            set { SetSize(value + value); }
        }

        /// <summary>
        /// The major radius for this ellipse.
        /// </summary>
        public float MajorRadius
        {
            get { return Width; }
            set { Width = value + value; }
        }

        /// <summary>
        /// The minor radius for this ellipse.
        /// </summary>
        public float MinorRadius
        {
            get { return Height; }
            set { Height = value + value; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new ellipse renderer for the given game.
        /// </summary>
        /// <param name="game">The game we will render for.</param>
        /// <param name="effectName">The shader to use for rendering the shape.</param>
        protected AbstractEllipse(Game game, string effectName)
            : base(game, effectName)
        {
        }

        #endregion
    }
}
