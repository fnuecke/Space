using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;

namespace Space.ScreenManagement.Screens.Gameplay
{
    /// <summary>
    /// Renderer class responsible for drawing the overall in-game background,
    /// i.e. the stars an debris behind ships and planets and stuff.
    /// </summary>
    public sealed class Background
    {
        #region Fields

        /// <summary>
        /// The local client, used to fetch player's position, to compute our
        /// translation.
        /// </summary>
        private readonly GameClient _client;

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// The overall background image (stars).
        /// </summary>
        private Texture2D _background;

        /// <summary>
        /// Layer to blot out some of the stars.
        /// </summary>
        private Texture2D _backgroundDarkMatter;

        /// <summary>
        /// Smaller debris, rendered in the background.
        /// </summary>
        private Texture2D _backgroundDebrisSmall;

        /// <summary>
        /// Larger debris, rendered nearly at the foreground (i.e. the layer
        /// game objects are on, including players' ships).
        /// </summary>
        private Texture2D _backgroundDebrisLarge;

        #endregion

        #region Constructor

        public Background(GameClient client)
        {
            _client = client;
        }
        
        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent( SpriteBatch spriteBatch, ContentManager content)
        {
            _spriteBatch = spriteBatch;

            _background = content.Load<Texture2D>("Textures/stars");
            _backgroundDarkMatter = content.Load<Texture2D>("Textures/dark_matter");
            _backgroundDebrisSmall = content.Load<Texture2D>("Textures/debris_small");
            _backgroundDebrisLarge = content.Load<Texture2D>("Textures/debris_large");
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draw the overall in-game background.
        /// </summary>
        /// <remarks>
        /// TODO: consider different "biomes" with different appearance
        /// (coloration, debris texture, brightness, ...) and slowly blending
        /// those.
        /// </remarks>
        public void Draw()
        {
            // Get local player position.
            Vector2 position = Vector2.Zero;
            var info = _client.GetPlayerShipInfo();
            if (info != null)
            {
                position = -info.Position;
            }

            // Get our viewport.
            var viewport = _spriteBatch.GraphicsDevice.Viewport;

            Vector2 backgroundPosition;
            Rectangle source;
            source.Width = viewport.Width + 1;
            source.Height = viewport.Height + 1;

            // Draw the background, tiled, with the given translation.
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);

            // First the overall background (the stars).
            backgroundPosition.X = position.X * 0.05f;
            backgroundPosition.Y = position.Y * 0.05f;
            source.X = -(int)(backgroundPosition.X);
            source.Y = -(int)(backgroundPosition.Y);
            backgroundPosition.X += source.X;
            backgroundPosition.Y += source.Y;

            _spriteBatch.Draw(_background, backgroundPosition, source, Color.White);

            // Then the layer of "dark matter" (blot out some areas of the
            // stars, to give a better feeling of depth).
            backgroundPosition.X = position.X * 0.1f;
            backgroundPosition.Y = position.Y * 0.1f;
            source.X = -(int)(backgroundPosition.X);
            source.Y = -(int)(backgroundPosition.Y);
            backgroundPosition.X += source.X;
            backgroundPosition.Y += source.Y;

            _spriteBatch.Draw(_backgroundDarkMatter, backgroundPosition, source, Color.White * 0.95f);

            // Next up some small debris, appearing to be further back.
            backgroundPosition.X = position.X * 0.65f;
            backgroundPosition.Y = position.Y * 0.65f;
            source.X = -(int)(backgroundPosition.X);
            source.Y = -(int)(backgroundPosition.Y);
            backgroundPosition.X += source.X;
            backgroundPosition.Y += source.Y;

            _spriteBatch.Draw(_backgroundDebrisSmall, backgroundPosition, source, Color.DarkSlateGray * 0.75f);

            // And then a layer with larger debris, almost on the actual layer
            // of the player, to give a near to correct feeling of speed when
            // there's nothing else around.
            backgroundPosition.X = position.X * 0.95f;
            backgroundPosition.Y = position.Y * 0.95f;
            source.X = -(int)(backgroundPosition.X);
            source.Y = -(int)(backgroundPosition.Y);
            backgroundPosition.X += source.X;
            backgroundPosition.Y += source.Y;

            _spriteBatch.Draw(_backgroundDebrisLarge, backgroundPosition, source, Color.SlateGray * 0.25f);

            _spriteBatch.End();
        }

        #endregion
    }
}
