using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Space.ScreenManagement.Screens.Helper {
    /// <summary>
    /// A helper class which helps drawing standard items like rectangles, circles, etc...
    /// </summary>
    class BasicForms
    {

        private Texture2D _t;
        private SpriteBatch _spriteBatch;

        public BasicForms(SpriteBatch spriteBatch)
        {
            _spriteBatch = spriteBatch;
            _t = new Texture2D(_spriteBatch.GraphicsDevice, 1, 1);
            _t.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Draws the borders of a rectangle.
        /// </summary>
        public void DrawRectangle(int x, int y, int width, int height, Color color)
        {
            _spriteBatch.Draw(_t, new Rectangle(x, y, 1, height), color); // Left
            _spriteBatch.Draw(_t, new Rectangle(x + width, y, 1, height), color); // Right
            _spriteBatch.Draw(_t, new Rectangle(x, y, width, 1), color); // Top
            _spriteBatch.Draw(_t, new Rectangle(x, y + height, width, 1), color); // Bottom
        }

        /// <summary>
        /// Draws a rectangle.
        /// </summary>
        public void FillRectangle(int x, int y, int width, int height, Color color)
        {
            _spriteBatch.Draw(_t, new Rectangle(x, y, width, height), color);
        }

    }
}
