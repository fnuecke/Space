using System;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.Graphics
{
    /// <summary>
    /// Utility class for rendering planets.
    /// </summary>
    sealed class Planet : AbstractShape
    {
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
        private GameTime _gameTime;

        #endregion

        #region Constructor

        public Planet(Game game)
            : base(game, "Shaders/Planet")
        {
            var random = new Random();
            _surfaceRotation.X = ((float)random.NextDouble() + 1f) / 2f;
            _surfaceRotation.Y = ((float)random.NextDouble() + 1f) / 2f;
            _surfaceRotation.Normalize();
            _surfaceRotation *= 5 * Math.Sign(random.NextDouble() - 0.5);
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Sets the surface texture.
        /// </summary>
        /// <param name="texture">The texture to use.</param>
        public void SetSurfaceTexture(Texture2D texture)
        {
            _surface = texture;
        }

        /// <summary>
        /// Sets the surface tint applied to the surface texture.
        /// </summary>
        /// <param name="tint">The tint to use.</param>
        public void SetSurfaceTint(Color tint)
        {
            SetColor(tint);
        }

        /// <summary>
        /// Sets the atmosphere tint applied to the planet's atmosphere.
        /// </summary>
        /// <param name="tint">The tint to use.</param>
        public void SetAtmosphereTint(Color tint)
        {
            _atmosphereTint = tint;
        }

        /// <summary>
        /// Sets the direction the light is coming from (vector to the sun
        /// this planet is orbiting).
        /// </summary>
        /// <param name="toLight">Direction to the light source.</param>
        public void SetLightDirection(ref Vector2 toLight)
        {
            _lightDirection = toLight;
        }

        /// <summary>
        /// Sets the direction the light is coming from (vector to the sun
        /// this planet is orbiting).
        /// </summary>
        /// <param name="toLight">Direction to the light source.</param>
        public void SetLightDirection(Vector2 toLight)
        {
            SetLightDirection(ref toLight);
        }

        /// <summary>
        /// Sets the current game time, which is used to determine the current
        /// rotation of the planet.
        /// </summary>
        /// <param name="gameTime"></param>
        public void SetGameTime(GameTime gameTime)
        {
            _gameTime = gameTime;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Adjusts effect parameters prior to the draw call.
        /// </summary>
        protected override void AdjustParameters()
        {
            _effect.Parameters["SurfaceTexture"].SetValue(_surface);
            _effect.Parameters["SurfaceTint"].SetValue(_color.ToVector4());
            _effect.Parameters["AtmosphereTint"].SetValue(_atmosphereTint.ToVector4());
            _effect.Parameters["LightDirection"].SetValue(_lightDirection);

            _effect.Parameters["RenderRadius"].SetValue(_width / 2f);
            _effect.Parameters["EmbossScale"].SetValue(1f / _width);

            _effect.Parameters["TextureOffset"].SetValue(_surfaceRotation * ((float)_gameTime.TotalGameTime.TotalSeconds / _width));
            _effect.Parameters["TextureScale"].SetValue((float)_surface.Width / (2f * _width));
        }

        #endregion
    }
}
