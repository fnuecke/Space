using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.Graphics
{
    /// <summary>
    /// Utility class for rendering planets.
    /// </summary>
    public sealed class Planet : AbstractShape
    {
        #region Properties

        /// <summary>
        /// The surface texture.
        /// </summary>
        public Texture2D SurfaceTexture
        {
            get { return _surface; }
            set { _surface = value; }
        }

        /// <summary>
        /// The surface tint applied to the surface texture.
        /// </summary>
        public Color SurfaceTint
        {
            get { return Color; }
            set { Color = value; }
        }

        /// <summary>
        /// The atmosphere tint applied to the planet's atmosphere.
        /// </summary>
        public Color AtmosphereTint
        {
            get { return _atmosphereTint; }
            set { _atmosphereTint = value; }
        }

        /// <summary>
        /// The rotational direction of the planet's surface.
        /// </summary>
        public Vector2 SurfaceRotation
        {
            get { return _surfaceRotation; }
            set { _surfaceRotation = value; }
        }

        /// <summary>
        /// The direction the light is coming from (vector to the sun
        /// this planet is orbiting).
        /// </summary>
        public Vector2 LightDirection
        {
            get { return _lightDirection; }
            set { _lightDirection = value; }
        }

        /// <summary>
        /// The current game time, which is used to determine the current
        /// rotation of the planet, in seconds.
        /// </summary>
        public float Time
        {
            get { return _time; }
            set { _time = value; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The texture used for rendering the planet surface.
        /// </summary>
        private Texture2D _surface;

        /// <summary>
        /// The tint used for the atmosphere.
        /// </summary>
        private Color _atmosphereTint;

        /// <summary>
        /// The direction the light's coming from (sun the planet orbits).
        /// </summary>
        private Vector2 _lightDirection;

        /// <summary>
        /// Rotation direction (and speed) of the base image.
        /// </summary>
        private Vector2 _surfaceRotation;

        /// <summary>
        /// The current game time to base our rotation on.
        /// </summary>
        private float _time;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Planet"/> class.
        /// </summary>
        /// <param name="content">The content manager to use for loading assets.</param>
        /// <param name="graphics">The graphics device to render to.</param>
        public Planet(ContentManager content, GraphicsDevice graphics)
            : base(content, graphics, "Shaders/Planet")
        {
        }

        #endregion

        #region Draw

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected override void AdjustParameters()
        {
            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            Effect.Parameters["SurfaceTexture"].SetValue(_surface);
            Effect.Parameters["SurfaceTint"].SetValue(Color.ToVector4());
            Effect.Parameters["AtmosphereTint"].SetValue(_atmosphereTint.ToVector4());
            Effect.Parameters["LightDirection"].SetValue(_lightDirection);

            Effect.Parameters["RenderRadius"].SetValue(Width / 2f);
            Effect.Parameters["EmbossScale"].SetValue(1f / Width);

            var offset = _surfaceRotation * _time / Width;
            offset.X %= _surface.Width;
            offset.Y %= _surface.Height;
            Effect.Parameters["TextureOffset"].SetValue(offset);
            Effect.Parameters["TextureScale"].SetValue(_surface.Width / (2f * Width));
        }

        #endregion
    }
}
