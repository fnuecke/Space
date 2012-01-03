﻿using System.Text;
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

        public override void Update(object parameterization)
        {
            // The position and orientation we're rendering at and in.
            var transform = Entity.GetComponent<Transform>();

            // Draw the texture based on our physics component.
            if (transform != null)
            {
                base.Update(parameterization);

                // Get parameterization in proper type.
                var p = (RendererParameterization)parameterization;

                // Load our atmosphere texture, if it's not set.
                if (_atmosphereTexture == null)
                {
                    _atmosphereTexture = p.Content.Load<Texture2D>("Textures/planet_atmo");
                }
                if (_shadowTexture == null)
                {
                    _shadowTexture = p.Content.Load<Texture2D>("Textures/planet_shadow");
                }

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

                p.SpriteBatch.Begin();
                p.SpriteBatch.Draw(_atmosphereTexture,
                    new Rectangle((int)transform.Translation.X + (int)p.Translation.X,
                                  (int)transform.Translation.Y + (int)p.Translation.Y,
                                  (int)(_atmosphereTexture.Width * Scale), (int)(_atmosphereTexture.Height * Scale)),
                    null, AtmosphereTint,
                    sunDirection,
                    new Vector2(_atmosphereTexture.Width / 2, _atmosphereTexture.Height / 2),
                    SpriteEffects.None, 0);

                p.SpriteBatch.Draw(_shadowTexture,
                    new Rectangle((int)transform.Translation.X + (int)p.Translation.X,
                                  (int)transform.Translation.Y + (int)p.Translation.Y,
                                  (int)(_shadowTexture.Width * Scale), (int)(_shadowTexture.Height * Scale)),
                    null, Color.White,
                    sunDirection,
                    new Vector2(_shadowTexture.Width / 2, _shadowTexture.Height / 2),
                    SpriteEffects.None, 0);

#if DEBUG
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Position: {0}\n", transform.Translation);
                sb.AppendFormat("Rotation: {0}\n", transform.Rotation);
                sb.AppendFormat("Scale: {0}\n", Scale);
                sb.AppendFormat("Angle to sun: {0}\n", (int)MathHelper.ToDegrees(-sunDirection));

                p.SpriteBatch.DrawString(p.Content.Load<SpriteFont>("Fonts/ConsoleFont"), sb, transform.Translation + p.Translation, Color.White);
#endif
                p.SpriteBatch.End();
            }
        }

        #endregion
    }
}
