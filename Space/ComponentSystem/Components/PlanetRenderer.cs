using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Graphics;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Special renderer for a planet or moon.
    /// 
    /// <para>
    /// Draws its atmosphere and shadow based on the sun it orbits.
    /// </para>
    /// </summary>
    public sealed class PlanetRenderer : AbstractRenderer
    {
        #region Fields

        /// <summary>
        /// The color tint of this planet's atmosphere.
        /// </summary>
        public Color AtmosphereTint;

        /// <summary>
        /// The renderer we use to render our planet.
        /// </summary>
        private Planet _planet;

        #endregion

        #region Constructor

        public PlanetRenderer(string planetTexture, Color planetTint,
            float planetRadius, Color atmosphereTint)
            : base(planetTexture, planetTint, planetRadius)
        {
            AtmosphereTint = atmosphereTint;
        }

        public PlanetRenderer()
            : this(string.Empty, Color.White, 0, Color.White)
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

                if (_planet == null)
                {
                    _planet = new Planet(args.Game);
                    _planet.SetSize(Scale * 2);
                    _planet.SetSurfaceTexture(_texture);
                    _planet.SetSurfaceTint(Tint);
                    _planet.SetAtmosphereTint(AtmosphereTint);
                }

                // Get the position at which to draw (in screen space).
                Vector2 position;
                position.X = transform.Translation.X + args.Transform.Translation.X;
                position.Y = transform.Translation.Y + args.Transform.Translation.Y;

                // Check if we're even visible.
                Rectangle clipRectangle = args.SpriteBatch.GraphicsDevice.Viewport.Bounds;
                clipRectangle.Inflate((int)(2 * Scale), (int)(2 * Scale));
                if (clipRectangle.Contains((int)position.X, (int)position.Y))
                {
                    // Get position relative to our sun, to rotate atmosphere and shadow.
                    Vector2 toSun = Vector2.Zero;
                    Entity sun = GetSun();
                    if (sun != null)
                    {
                        var sunTransform = sun.GetComponent<Transform>();
                        if (sunTransform != null)
                        {
                            toSun = sunTransform.Translation - transform.Translation;
                            Matrix matrix = Matrix.CreateRotationZ(-transform.Rotation);
                            Vector2.Transform(ref toSun, ref matrix, out toSun);
                            toSun.Normalize();
                        }
                    }

                    _planet.SetCenter(position);
                    _planet.SetLightDirection(toSun);
                    _planet.SetRotation(transform.Rotation);
                    _planet.SetGameTime(args.GameTime);
                    _planet.Draw();

#if DEBUG
                    var sb = new System.Text.StringBuilder();
                    sb.AppendFormat("Position: {0}\n", transform.Translation);
                    sb.AppendFormat("Rotation: {0}\n", (int)MathHelper.ToDegrees(transform.Rotation));
                    sb.AppendFormat("Scale: {0}\n", Scale);
                    if (Entity.GetComponent<Gravitation>() != null)
                    {
                        sb.AppendFormat("Mass: {0:f}\n", Entity.GetComponent<Gravitation>().Mass);
                    }
                    args.SpriteBatch.Begin();
                    args.SpriteBatch.DrawString(args.Game.Content.Load<SpriteFont>("Fonts/ConsoleFont"), sb, position, Color.White);
                    args.SpriteBatch.End();
#endif
                }
            }
        }

        /// <summary>
        /// Utility method to find the sun we're rotating around.
        /// </summary>
        /// <returns></returns>
        private Entity GetSun()
        {
            Entity sun = null;
            var ellipse = Entity.GetComponent<EllipsePath>();
            while (ellipse != null)
            {
                sun = Entity.Manager.GetEntity(ellipse.CenterEntityId);
                ellipse = sun.GetComponent<EllipsePath>();
            }
            return sun;
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
                copy._planet = _planet;
            }

            return copy;
        }

        #endregion
    }
}
