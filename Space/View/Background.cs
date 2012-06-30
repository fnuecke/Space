using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Systems;
using Space.Control;

namespace Space.View
{
    /// <summary>
    /// Renderer class responsible for drawing the overall in-game background,
    /// i.e. the stars an debris behind ships and planets and stuff.
    /// </summary>
    public sealed class Background : AbstractShape
    {
        #region Properties

        /// <summary>
        /// The local client, used to fetch player's position, to compute our
        /// translation.
        /// </summary>
        public GameClient Client { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Sprite batch used for rendering.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// The overall background image (stars).
        /// </summary>
        private readonly Texture2D _backgroundStars;

        /// <summary>
        /// Layer to blot out some of the stars.
        /// </summary>
        private readonly Texture2D _backgroundDarkMatter;

        /// <summary>
        /// Smaller debris, rendered in the background.
        /// </summary>
        private readonly Texture2D _backgroundDebrisSmall;

        /// <summary>
        /// Larger debris, rendered nearly at the foreground (i.e. the layer
        /// game objects are on, including players' ships).
        /// </summary>
        private readonly Texture2D _backgroundDebrisLarge;

        #endregion

        #region Constructor

        public Background(Game game, SpriteBatch spriteBatch)
            : base(game, "Shaders/Space")
        {
            _spriteBatch = spriteBatch;

            _backgroundStars = game.Content.Load<Texture2D>("Textures/Space/stars");
            _backgroundDarkMatter = game.Content.Load<Texture2D>("Textures/Space/dark_matter");
            _backgroundDebrisSmall = game.Content.Load<Texture2D>("Textures/Space/debris_small");
            _backgroundDebrisLarge = game.Content.Load<Texture2D>("Textures/Space/debris_large");

            // Normally we'd want to set the parameters each draw call, but we
            // only use this shader in one place, so it's OK to do this once.
            Effect.Parameters["Stars"].SetValue(_backgroundStars);
            Effect.Parameters["DarkMatter"].SetValue(_backgroundDarkMatter);
            Effect.Parameters["DebrisSmall"].SetValue(_backgroundDebrisSmall);
            Effect.Parameters["DebrisLarge"].SetValue(_backgroundDebrisLarge);

            Effect.Parameters["DebrisSmallTint"].SetValue(Color.DarkSlateGray.ToVector4());
            Effect.Parameters["DebrisLargeTint"].SetValue(Color.SlateGray.ToVector4());

            // Set our size, which is simply the viewport, but keep it a square
            // to avoid stretching of the background. Scale it up, so that it still
            // fills the whole screen, even when zoomed out completely.
            var viewport = _spriteBatch.GraphicsDevice.Viewport;
            var maxsize = System.Math.Max(viewport.Width / CameraSystem.MinimumZoom,
                                          viewport.Height / CameraSystem.MinimumZoom);
            SetSize(maxsize);
            SetCenter(viewport.Width / 2f, viewport.Height / 2f);

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
            if (Client != null)
            {
                Effect.Parameters["Position"].SetValue(Client.GetCameraPosition() / Width);
            }
        }

        #endregion
    }
}
