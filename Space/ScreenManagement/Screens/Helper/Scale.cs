using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Space.ScreenManagement.Screens.Helper
{

    /// <summary>
    /// It's the comeback of the incredible Gnulf and he brings along his best
    /// knowledges in scaling values for the graphical user interface. At the
    /// request of Gnulf the class was renamed into a more serious class name
    /// due to reasons of masking.
    /// 
    /// The elements of the graphical user interface that should be scaled have
    /// to call one of the scaling methods which will return the scaled values.
    /// </summary>
    public class Scale
    {

        #region Fields

        /// <summary>
        /// The height of the standard resolution.
        /// The graphical user interface is optimated for this resolution.
        /// </summary>
        private Vector2 _standardResolution = new Vector2(1280, 800);

        /// <summary>
        /// Holds the actual resolution.
        /// </summary>
        private Vector2 _actualResolution;

        #endregion

        #region Initialisation

        /// <summary>
        /// Constructor
        /// </summary>
        public Scale(SpriteBatch spriteBatch)
        {
            _actualResolution = new Vector2(spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height);
        }

        #endregion

        #region Scaling methods

        /// <summary>
        /// Returns the scaled value for the variable's position.
        /// </summary>
        /// <param name="value">The value that should be scaled.</param>
        /// <returns>The scaled value</returns>
        public int X(float value)
        {
            return (int)(value * GetScaleXValue());
        }

        /// <summary>
        /// Returns the scaled value for the variable's position.
        /// </summary>
        /// <param name="value">The value that should be scaled.</param>
        /// <returns>The scaled value</returns>
        public int Y(float value)
        {
            return (int)(value * GetScaleYValue());
        }

        /// <summary>
        /// Returns the x scaling value.
        /// </summary>
        /// <returns></returns>
        public float GetScaleXValue()
        {
            return _actualResolution.X / _standardResolution.X;
        }

        /// <summary>
        /// Returns the y scaling value.
        /// </summary>
        /// <returns></returns>
        public float GetScaleYValue()
        {
            return _actualResolution.Y / _standardResolution.Y;
        }

        #endregion

    }
}
