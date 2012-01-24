using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ScreenManagement.Screens.Helper
{
    class SpaceForms
    {

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// A Texture2D object that holds an 1x1 pixel which is used for the elements.
        /// </summary>
        private Texture2D _t;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SpaceForms(SpriteBatch spriteBatch)
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
        /// <param name="sizeEdges">The size of the edges.</param>
        /// <param name="gap">The size of the gap between rectangle and edges.</param>
        /// <param name="color">The color of the rectangle. Please only use colors with a 100% alpha value.</param>
        public void DrawRectangleWithoutEdges(int x, int y, int width, int height, int sizeEdges, int gap, Color color)
        {
            // draw the side lines
            _spriteBatch.Draw(_t, new Rectangle(x, y + gap + sizeEdges, 1, height - 2 * gap - 2 * sizeEdges), color * 0.3f); // Left
            _spriteBatch.Draw(_t, new Rectangle(x + width - 1, y + gap + sizeEdges, 1, height - 2 * gap - 2 * sizeEdges), color * 0.3f); // Right
            _spriteBatch.Draw(_t, new Rectangle(x + gap + sizeEdges, y, width - 2 * gap - 2 * sizeEdges, 1), color * 0.3f); // Top
            _spriteBatch.Draw(_t, new Rectangle(x + gap + sizeEdges, y + height - 1, width - 2 * gap - 2 * sizeEdges, 1), color * 0.3f); // Bottom

            // draw the edges
            _spriteBatch.Draw(_t, new Rectangle(x, y, 1, sizeEdges), color * 0.8f); // Top Left
            _spriteBatch.Draw(_t, new Rectangle(x + 1, y, sizeEdges - 1, 1), color * 0.8f);

            _spriteBatch.Draw(_t, new Rectangle(x + width - 1, y, 1, sizeEdges), color * 0.8f); // Top Right
            _spriteBatch.Draw(_t, new Rectangle(x + width - sizeEdges, y, sizeEdges - 1, 1), color * 0.8f);

            _spriteBatch.Draw(_t, new Rectangle(x, y + height - sizeEdges, 1, sizeEdges), color * 0.8f); // Bottom Left
            _spriteBatch.Draw(_t, new Rectangle(x + 1, y + height - 1, sizeEdges - 1, 1), color * 0.8f);

            _spriteBatch.Draw(_t, new Rectangle(x + width - 1, y + height - sizeEdges, 1, sizeEdges), color * 0.8f); // Bottom Right
            _spriteBatch.Draw(_t, new Rectangle(x + width - sizeEdges, y + height - 1, sizeEdges - 1, 1), color * 0.8f);
        }
    }
}
