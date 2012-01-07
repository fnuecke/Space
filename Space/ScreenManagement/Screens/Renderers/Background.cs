using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.Control;

namespace Space.ScreenManagement.Screens.Renderers
{
    public sealed class Background
    {
        #region Fields

        private readonly GameClient _client;

        private SpriteBatch _spriteBatch;

        private Texture2D _background;

        private Texture2D _backgroundDarkMatter;

        private Texture2D _backgroundDebrisSmall;

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
            var avatar = _client.GetAvatar();
            if (avatar != null)
            {
                position = -avatar.GetComponent<Transform>().Translation;
            }

            // Get our viewport.
            var viewport = _spriteBatch.GraphicsDevice.Viewport;

            // Draw the background, tiled, with the given translation.
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);

            // First the overall background (the stars).
            _spriteBatch.Draw(_background, Vector2.Zero,
                new Rectangle(-(int)(position.X * 0.05f), -(int)(position.Y * 0.05f), viewport.Width, viewport.Height),
                    Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Then the layer of "dark matter" (blot out some areas of the
            // stars, to give a better feeling of depth).
            _spriteBatch.Draw(_backgroundDarkMatter, Vector2.Zero,
                new Rectangle(-(int)(position.X * 0.1f), -(int)(position.Y * 0.1f), viewport.Width, viewport.Height),
                    Color.White * 0.95f, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Next up some small debris, appearing to be further back.
            _spriteBatch.Draw(_backgroundDebrisSmall, Vector2.Zero,
                new Rectangle(-(int)(position.X * 0.65f), -(int)(position.Y * 0.65f),
                    viewport.Width,
                    viewport.Height),
                    Color.DarkSlateGray * 0.75f, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // And then a layer with larger debris, almost on the actual layer
            // of the player, to give a near to correct feeling of speed when
            // there's nothing else around.
            _spriteBatch.Draw(_backgroundDebrisLarge, Vector2.Zero,
                new Rectangle(-(int)(position.X * 0.95f), -(int)(position.Y * 0.95f), viewport.Width, viewport.Height),
                    Color.SlateGray * 0.25f, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            _spriteBatch.End();
        }

        #endregion
    }
}
