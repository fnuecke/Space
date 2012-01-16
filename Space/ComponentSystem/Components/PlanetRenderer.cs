using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Special renderer for a planet or moon.
    /// 
    /// <para>
    /// Draws its atmosphere and shadow based on the sun it orbits.
    /// </para>
    /// </summary>
    public sealed class PlanetRenderer : TransformedRenderer
    {
        #region Fields

        /// <summary>
        /// The color tint of this planet's atmosphere.
        /// </summary>
        public Color AtmosphereTint;

        /// <summary>
        /// The texture used for the atmosphere.
        /// </summary>
        private Texture2D _atmosphereTexture;

        /// <summary>
        /// The texture used for the shadow.
        /// </summary>
        private Texture2D _shadowTexture;

        #endregion

        #region Constructor
        
        public PlanetRenderer(string textureName, Color atmosphereTint)
            : base(textureName)
        {
            AtmosphereTint = atmosphereTint;
        }

        public PlanetRenderer(string textureName)
            : this(textureName, Color.PaleTurquoise)
        {
        }

        public PlanetRenderer()
            : this(string.Empty)
        {
        }

        #endregion

        #region Logic

        public override void Draw(object parameterization)
        {
            // The position and orientation we're rendering at and in.
            var transform = Entity.GetComponent<Transform>();

            // Draw the texture based on our physics component.
            if (transform != null)
            {
                base.Draw(parameterization);

                // Get parameterization in proper type.
                var args = (RendererParameterization)parameterization;

                // Load our atmosphere texture, if it's not set.
                if (_atmosphereTexture == null)
                {
                    _atmosphereTexture = args.Content.Load<Texture2D>("Textures/planet_atmo");
                }
                if (_shadowTexture == null)
                {
                    _shadowTexture = args.Content.Load<Texture2D>("Textures/planet_shadow2");
                }

                // Get the rectangles at which we'll draw. These are offset
                // by half the texture size for the following test.
                Vector2 atmosphereOrigin;
                atmosphereOrigin.X = _atmosphereTexture.Width / 2;
                atmosphereOrigin.Y = _atmosphereTexture.Height / 2;
                Rectangle atmosphereDestination;
                atmosphereDestination.X = (int)(transform.Translation.X - atmosphereOrigin.X * Scale);
                atmosphereDestination.Y = (int)(transform.Translation.Y - atmosphereOrigin.Y * Scale);
                atmosphereDestination.Width = (int)(_atmosphereTexture.Width * Scale);
                atmosphereDestination.Height = (int)(_atmosphereTexture.Height * Scale);

                Vector2 shadowOrigin;
                shadowOrigin.X = _shadowTexture.Width * 3 / 4;
                shadowOrigin.Y = _shadowTexture.Height / 2;
                Rectangle shadowDestination;
                shadowDestination.X = (int)(transform.Translation.X - shadowOrigin.X * Scale);
                shadowDestination.Y = (int)(transform.Translation.Y - shadowOrigin.Y * Scale);
                shadowDestination.Width = (int)(_shadowTexture.Width * Scale);
                shadowDestination.Height = (int)(_shadowTexture.Height * Scale);

                // Are they within our screen space? Use a somewhat loosened
                // up clipping rectangle, to account for rotated and non-
                // rectangular textures.
                // Note that this is mainly a performance gain because we can
                // avoid computing the directions to the suns this way.
                Rectangle looseClipRectangle = args.SpriteBatch.GraphicsDevice.Viewport.Bounds;
                looseClipRectangle.Inflate(512, 512);
                looseClipRectangle.Offset(-(int)args.Transform.Translation.X, -(int)args.Transform.Translation.Y);
                var atmosphereVisible = atmosphereDestination.Intersects(looseClipRectangle);
                var shadowVisible = shadowDestination.Intersects(looseClipRectangle);

                // If either is, carry on.
                if (atmosphereVisible || shadowVisible)
                {
                    // Get position relative to our sun, to rotate atmosphere and shadow.
                    float sunDirection = 0;
                    Entity sun = null;
                    var ellipse = Entity.GetComponent<EllipsePath>();
                    while (ellipse != null)
                    {
                        sun = Entity.Manager.GetEntity(ellipse.CenterEntityId);
                        ellipse = sun.GetComponent<EllipsePath>();
                    }
                    if (sun != null)
                    {
                        var sunTransform = sun.GetComponent<Transform>();
                        if (sunTransform != null)
                        {
                            var toSun = sunTransform.Translation - transform.Translation;
                            sunDirection = (float)System.Math.Atan2(toSun.Y, toSun.X);
                        }
                    }

                    var position = transform.Translation;
                    position.X += args.Transform.Translation.X;
                    position.Y += args.Transform.Translation.Y;

                    // Draw whatever is visible.
                    if (atmosphereVisible)
                    {
                        // Draw.
                        args.SpriteBatch.Draw(_atmosphereTexture, position, null, AtmosphereTint,
                            sunDirection, atmosphereOrigin, Scale, SpriteEffects.None, 0);
                    }

                    if (shadowVisible)
                    {
                        // Draw.
                        args.SpriteBatch.Draw(_shadowTexture, position, null, Color.White,
                            sunDirection, shadowOrigin, Scale, SpriteEffects.None, 0);
                    }

#if DEBUG
                    var sb = new System.Text.StringBuilder();
                    sb.AppendFormat("Position: {0}\n", transform.Translation);
                    sb.AppendFormat("Rotation: {0}\n", (int)MathHelper.ToDegrees(transform.Rotation));
                    sb.AppendFormat("Scale: {0}\n", Scale);
                    sb.AppendFormat("Angle to sun: {0}\n", (int)MathHelper.ToDegrees(-sunDirection));
                    sb.AppendFormat("Mass: {0:f}\n", Entity.GetComponent<Gravitation>().Mass);

                    args.SpriteBatch.DrawString(args.Content.Load<SpriteFont>("Fonts/ConsoleFont"), sb, transform.Translation, Color.White);
#endif
                }
            }
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (PlanetRenderer)base.DeepCopy(into);

            if (copy == into)
            {
                copy.AtmosphereTint = AtmosphereTint;
            }

            return copy;
        }

        #endregion
    }
}
