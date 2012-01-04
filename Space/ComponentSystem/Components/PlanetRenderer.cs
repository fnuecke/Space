using System.Text;
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
        #region Properties
        
        /// <summary>
        /// The color tint of this planet's atmosphere.
        /// </summary>
        public Color AtmosphereTint { get; set; }

        #endregion

        #region Fields

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
                    _shadowTexture = args.Content.Load<Texture2D>("Textures/planet_shadow");
                }

                var atmosphereDestination = new Rectangle(
                    (int)transform.Translation.X + (int)args.Transform.Translation.X,
                    (int)transform.Translation.Y + (int)args.Transform.Translation.Y,
                    (int)(_atmosphereTexture.Width * Scale), (int)(_atmosphereTexture.Height * Scale));

                var shadowDestination = new Rectangle(
                    (int)transform.Translation.X + (int)args.Transform.Translation.X,
                    (int)transform.Translation.Y + (int)args.Transform.Translation.Y,
                    (int)(_atmosphereTexture.Width * Scale), (int)(_atmosphereTexture.Height * Scale));

                var atmosphereVisible = atmosphereDestination.Intersects(args.SpriteBatch.GraphicsDevice.ScissorRectangle);
                var shadowVisible = atmosphereDestination.Intersects(args.SpriteBatch.GraphicsDevice.ScissorRectangle);

                if (atmosphereVisible || shadowVisible)
                {
                    // Get position relative to our sun, to rotate atmosphere and shadow.
                    float sunDirection = 0;
                    IEntity sun = null;
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

                    args.SpriteBatch.Begin();
                    if (atmosphereVisible)
                    {
                        args.SpriteBatch.Draw(_atmosphereTexture, atmosphereDestination,
                            null, AtmosphereTint, sunDirection,
                            new Vector2(_atmosphereTexture.Width / 2, _atmosphereTexture.Height / 2),
                            SpriteEffects.None, 0);
                    }

                    if (shadowVisible)
                    {
                        args.SpriteBatch.Draw(_shadowTexture, shadowDestination,
                            null, Color.White, sunDirection,
                            new Vector2(_shadowTexture.Width / 2, _shadowTexture.Height / 2),
                            SpriteEffects.None, 0);
                    }

#if DEBUG
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("Position: {0}\n", transform.Translation);
                    sb.AppendFormat("Rotation: {0}\n", transform.Rotation);
                    sb.AppendFormat("Scale: {0}\n", Scale);
                    sb.AppendFormat("Angle to sun: {0}\n", (int)MathHelper.ToDegrees(-sunDirection));

                    args.SpriteBatch.DrawString(args.Content.Load<SpriteFont>("Fonts/ConsoleFont"), sb, transform.Translation + new Vector2(args.Transform.Translation.X, args.Transform.Translation.Y), Color.White);
#endif
                    args.SpriteBatch.End();
                }
            }
        }

        #endregion
    }
}
