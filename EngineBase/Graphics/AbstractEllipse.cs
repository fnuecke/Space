using Microsoft.Xna.Framework;

namespace Engine.Graphics
{
    /// <summary>
    /// Base class for ellipse / circle shape renderers.
    /// </summary>
    public abstract class AbstractEllipse : AbstractShape
    {
        #region Constructor

        /// <summary>
        /// Creates a new ellipse renderer for the given game.
        /// </summary>
        /// <param name="game"></param>
        protected AbstractEllipse(Game game, string effectName)
            : base(game, effectName)
        {
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets a new major and minor radius for this ellipse, i.e. will make
        /// this a circle with the given radius.
        /// </summary>
        /// <param name="radius">The new radius.</param>
        public void SetRadius(float radius)
        {
            SetSize(radius + radius);
        }

        /// <summary>
        /// Sets a new major radius for this ellipse.
        /// </summary>
        /// <param name="radius">The new major radius.</param>
        public void SetMajorRadius(float radius)
        {
            SetWidth(radius + radius);
        }

        /// <summary>
        /// Sets a new minor radius for this ellipse.
        /// </summary>
        /// <param name="radius">The new minor radius.</param>
        public void SetMinorRadius(float radius)
        {
            SetHeight(radius + radius);
        }

        #endregion
    }
}
