using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Components
{
    public class PlanetRenderer : TransformedRenderer
    {
        public Color AtmosphereTint { get; set; }

        #region Fields

        /// <summary>
        /// The texture used for the atmosphere.
        /// </summary>
        protected Texture2D atmosphereTexture;

        /// <summary>
        /// The texture used for the shadow.
        /// </summary>
        protected Texture2D shadowTexture;

        #endregion

        public PlanetRenderer()
        {
            AtmosphereTint = Color.PaleTurquoise;
        }

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
                if (atmosphereTexture == null)
                {
                    atmosphereTexture = p.Content.Load<Texture2D>("Textures/planet_atmo");
                }
                if (shadowTexture == null)
                {
                    shadowTexture = p.Content.Load<Texture2D>("Textures/planet_shadow");
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
                        sunDirection = (float)System.Math.Atan2(
                            sunTransform.Translation.Y - transform.Translation.Y,
                            sunTransform.Translation.X - transform.Translation.Y);
                    }
                }

                p.SpriteBatch.Begin();
                p.SpriteBatch.Draw(atmosphereTexture,
                    new Rectangle((int)transform.Translation.X + (int)p.Translation.X,
                                  (int)transform.Translation.Y + (int)p.Translation.Y,
                                  (int)(atmosphereTexture.Width * Scale), (int)(atmosphereTexture.Height * Scale)),
                    null, AtmosphereTint,
                    sunDirection,
                    new Vector2(atmosphereTexture.Width / 2, atmosphereTexture.Height / 2),
                    SpriteEffects.None, 0);

                p.SpriteBatch.Draw(shadowTexture,
                    new Rectangle((int)transform.Translation.X + (int)p.Translation.X,
                                  (int)transform.Translation.Y + (int)p.Translation.Y,
                                  (int)(shadowTexture.Width * Scale), (int)(shadowTexture.Height * Scale)),
                    null, Color.White,
                    sunDirection,
                    new Vector2(shadowTexture.Width / 2, shadowTexture.Height / 2),
                    SpriteEffects.None, 0);
                p.SpriteBatch.End();
            }
        }
    }
}
