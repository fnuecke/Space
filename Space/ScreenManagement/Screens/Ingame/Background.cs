using Engine.Graphics;
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
    public sealed class Background : AbstractShape
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
        private Texture2D _backgroundStars;

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
            : base(client.Game, "Shaders/Space")
        {
            _client = client;
        }
        
        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public void LoadContent( SpriteBatch spriteBatch, ContentManager content)
        {
            _spriteBatch = spriteBatch;

            _backgroundStars = content.Load<Texture2D>("Textures/Space/stars");
            _backgroundDarkMatter = content.Load<Texture2D>("Textures/Space/dark_matter");
            _backgroundDebrisSmall = content.Load<Texture2D>("Textures/Space/debris_small");
            _backgroundDebrisLarge = content.Load<Texture2D>("Textures/Space/debris_large");

            // Normally we'd want to set the parameters each draw call, but we
            // only use this shader in one place, so it's OK to do this once.
            _effect.Parameters["Stars"].SetValue(_backgroundStars);
            _effect.Parameters["DarkMatter"].SetValue(_backgroundDarkMatter);
            _effect.Parameters["DebrisSmall"].SetValue(_backgroundDebrisSmall);
            _effect.Parameters["DebrisLarge"].SetValue(_backgroundDebrisLarge);

            _effect.Parameters["DebrisSmallTint"].SetValue(Color.DarkSlateGray.ToVector4());
            _effect.Parameters["DebrisLargeTint"].SetValue(Color.SlateGray.ToVector4());

            // Set our size, which is simply the viewport, but keep it a square
            // to avoid stretching of the background.
            var viewport = _spriteBatch.GraphicsDevice.Viewport;
            var maxsize = System.Math.Max(viewport.Width, viewport.Width);
            SetSize(maxsize);
            SetCenter(viewport.Width / 2, viewport.Height / 2);

            // Adjust texture coordinates.
            _vertices[0].Tex0.X = 0;
            _vertices[0].Tex0.Y = 0;
            _vertices[1].Tex0.X = 1;
            _vertices[1].Tex0.Y = 0;
            _vertices[2].Tex0.X = 0;
            _vertices[2].Tex0.Y = 1;
            _vertices[3].Tex0.X = 1;
            _vertices[3].Tex0.Y = 1;
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected override void AdjustParameters()
        {
            // Get local camera position.
            _effect.Parameters["Position"].SetValue(_client.GetCameraPosition() / _width);
        }

        #endregion
    }
}
