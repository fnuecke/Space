using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Space.ScreenManagement.Screens.Helper {
    /// <summary>
    /// A helper class which helps drawing standard items like rectagles, circles, etc...
    /// </summary>
    class BasicForms
    {

        /// <summary>
        /// Draws the borders of a rectangle.
        /// </summary>
        public static void DrawRectangle(SpriteBatch spriteBatch, int x, int y, int width, int height, Color color)
        {
            var t = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            t.SetData(new[] { color });
            spriteBatch.Draw(t, new Rectangle(x, y, 1, height), color); // Left
            spriteBatch.Draw(t, new Rectangle(x + width, y, 1, height), color); // Right
            spriteBatch.Draw(t, new Rectangle(x, y, width, 1), color); // Top
            spriteBatch.Draw(t, new Rectangle(x, y + height, width, 1), color); // Bottom
        }

        /// <summary>
        /// Draws a rectangle.
        /// </summary>
        public static void FillRectangle(SpriteBatch spriteBatch, int x, int y, int width, int height, Color color)
        {
            var t = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            t.SetData(new[] { Color.White });
            spriteBatch.Draw(t, new Rectangle(x, y, width, height), color);
        }

    }
}
