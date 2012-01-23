using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Space.Control;

namespace Space.ScreenManagement.Screens.Helper {
    /// <summary>
    /// A helper class which helps drawing standard items like rectangles, circles, etc...
    /// </summary>
    public class BasicForms
    {

        /// <summary>
        /// The local client, used to fetch player's position and radar range.
        /// </summary>
        private readonly GameClient _client;

        /// <summary>
        /// A Texture2D object that holds an 1x1 pixel which is used for the elements.
        /// </summary>
        private Texture2D _t;

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        private Engine.Graphics.GradientRectangle _gradient;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BasicForms(SpriteBatch spriteBatch, GameClient client)
        {
            _spriteBatch = spriteBatch;
            _client = client;

            _gradient = new Engine.Graphics.GradientRectangle(_client.Game);
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

        public void GradientRectangle(int x, int y, int width, int height, Color color1, Color color2) {
            GradientRectangle(x, y, width, height, new[] { color1, color2 }, new[] { 0.0f, 1.0f });
        }

        public void GradientRectangle(int x, int y, int width, int height, Color[] colors, float[] range)
        {
            _gradient.SetCenter(x + width / 2, y + height / 2);
            _gradient.SetSize(width, height);
            _gradient.SetGradients(colors, range);
            _gradient.Draw();
        }
    }
}
