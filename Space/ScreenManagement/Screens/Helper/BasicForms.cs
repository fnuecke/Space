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

        /// <summary>
        /// A Texture2D object that holds an 1x1 pixel which is used for the elements.
        /// </summary>
        private Texture2D _t;

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BasicForms(SpriteBatch spriteBatch)
        {
            _spriteBatch = spriteBatch;
            _t = new Texture2D(_spriteBatch.GraphicsDevice, 1, 1);
            _t.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Draws the borders of a rectangle.
        /// </summary>
        /// <param name="x">The top-left x-coordinate where to draw the element.</param>
        /// <param name="y">The top-left y-coordinate where to draw the element.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The right of the rectangle.</param>
        /// <param name="color">The color of the rectangle.</param>
        public void DrawRectangle(int x, int y, int width, int height, Color color)
        {
            _spriteBatch.Draw(_t, new Rectangle(x, y + 1, 1, height - 2), color); // Left
            _spriteBatch.Draw(_t, new Rectangle(x + width - 1, y + 1, 1, height - 2), color); // Right
            _spriteBatch.Draw(_t, new Rectangle(x, y, width, 1), color); // Top
            _spriteBatch.Draw(_t, new Rectangle(x, y + height - 1, width, 1), color); // Bottom
        }

        /// <summary>
        /// Draws a rectangle and fill it with a color.
        /// </summary>
        /// <param name="x">The top-left x-coordinate where to draw the element.</param>
        /// <param name="y">The top-left y-coordinate where to draw the element.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The right of the rectangle.</param>
        /// <param name="color">The color of the rectangle.</param>
        public void FillRectangle(int x, int y, int width, int height, Color color)
        {
            _spriteBatch.Draw(_t, new Rectangle(x, y, width, height), color);
        }

        public void GradientRectangle() { 
//            var gr = new GradientRectangle(game); // irgendwo in nem feld halten, nicht immer neu anlegen.
//            gr.SetCenter(512, 512);
//            gr.SetSize(512);
//            gr.SetGradients(new[] { Color.Yellow, Color.Blue });
//            // oder
//            gr.SetGradients(new[] { Color.Red, Color.Green, Color.Blue }, new[] { 0.5f, 0.7f, 0.9f });
//            gr.Draw()
        }
    }
}
