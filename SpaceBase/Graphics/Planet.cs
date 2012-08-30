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
        /// Gets or sets the surface lights texture.
        /// </summary>
        public Texture2D SurfaceLights
        {
            get { return _lights; }
            set { _lights = value; }
        }

        /// <summary>
        /// Gets or sets the surface normals texture.
        /// </summary>
        public Texture2D SurfaceNormals
        {
            get { return _normals; }
            set { _normals = value; }
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
        /// Relative inner atmosphere area.
        /// </summary>
        public float AtmosphereInner
        {
            get { return _atmosphereInner; }
            set { _atmosphereInner = value; }
        }

        /// <summary>
        /// Relative outer atmosphere area.
        /// </summary>
        public float AtmosphereOuter
        {
            get { return _atmosphereOuter; }
            set { _atmosphereOuter = value; }
        }

        /// <summary>
        /// Relative inner atmosphere alpha.
        /// </summary>
        public float AtmosphereInnerAlpha
        {
            get { return _atmosphereInnerAlpha; }
            set { _atmosphereInnerAlpha = value; }
        }

        /// <summary>
        /// Relative outer atmosphere alpha.
        /// </summary>
        public float AtmosphereOuterAlpha
        {
            get { return _atmosphereOuterAlpha; }
            set { _atmosphereOuterAlpha = value; }
        }

        /// <summary>
        /// The rotational speed of the planet's surface along the x-axis.
        /// </summary>
        public float SurfaceRotation
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
        /// The surface lights texture.
        /// </summary>
        private Texture2D _lights;

        /// <summary>
        /// The surface normals texture.
        /// </summary>
        private Texture2D _normals;

        /// <summary>
        /// The tint used for the atmosphere.
        /// </summary>
        private Color _atmosphereTint;

        /// <summary>
        /// Relative inner atmosphere area.
        /// </summary>
        private float _atmosphereInner = 0.4f;

        /// <summary>
        /// Relative outer atmosphere area.
        /// </summary>
        private float _atmosphereOuter = 0.1f;

        /// <summary>
        /// Relative inner atmosphere alpha.
        /// </summary>
        private float _atmosphereInnerAlpha = 0.85f;

        /// <summary>
        /// Relative outer atmosphere alpha.
        /// </summary>
        private float _atmosphereOuterAlpha = 1f;

        /// <summary>
        /// The direction the light's coming from (sun the planet orbits).
        /// </summary>
        private Vector2 _lightDirection;

        /// <summary>
        /// Rotation speed of the base image along the x-axis.
        /// </summary>
        private float _surfaceRotation;

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
            var value = Effect.Parameters["SurfaceTexture"];
            if (value != null)
            {
                value.SetValue(_surface);
            }
            value = Effect.Parameters["SurfaceLights"];
            if (value != null)
            {
                value.SetValue(_lights);
                var flag = Effect.Parameters["HasLights"];
                if (flag != null)
                {
                    flag.SetValue(_lights != null);
                }
            }
            value = Effect.Parameters["SurfaceNormals"];
            if (value != null)
            {
                value.SetValue(_normals);
                var flag = Effect.Parameters["HasNormals"];
                if (flag != null)
                {
                    flag.SetValue(_normals != null);
                }
            }
            value = Effect.Parameters["SurfaceTint"];
            if (value != null)
            {
                value.SetValue(Color.ToVector4());
            }

            value = Effect.Parameters["AtmosphereTint"];
            if (value != null)
            {
                value.SetValue(_atmosphereTint.ToVector4());
            }
            value = Effect.Parameters["AtmosphereInner"];
            if (value != null)
            {
                value.SetValue(_atmosphereInner);
            }
            value = Effect.Parameters["AtmosphereOuter"];
            if (value != null)
            {
                value.SetValue(_atmosphereOuter);
            }
            value = Effect.Parameters["AtmosphereInnerAlpha"];
            if (value != null)
            {
                value.SetValue(_atmosphereInnerAlpha);
            }
            value = Effect.Parameters["AtmosphereOuterAlpha"];
            if (value != null)
            {
                value.SetValue(_atmosphereOuterAlpha);
            }

            value = Effect.Parameters["LightDirection"];
            if (value != null)
            {
                value.SetValue(_lightDirection);
            }
            value = Effect.Parameters["TextureOffset"];
            if (value != null)
            {
                value.SetValue((_surfaceRotation / MathHelper.Pi * _time) % 2f);
            }
            value = Effect.Parameters["RenderRadius"];
            if (value != null)
            {
                value.SetValue(Width / 2f);
            }
            value = Effect.Parameters["HorizontalScale"];
            if (value != null)
            {
                value.SetValue(_surface.Height / (float)_surface.Width);
            }
        }

        #endregion
    }
}
