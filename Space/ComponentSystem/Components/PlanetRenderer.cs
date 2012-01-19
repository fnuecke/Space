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
    public sealed class PlanetRenderer : AbstractRenderer
    {
        #region Fields

        /// <summary>
        /// The color tint of this planet's atmosphere.
        /// </summary>
        public Color AtmosphereTint;

        /// <summary>
        /// The direction of this planets rotation, i.e. the normal to the
        /// planets rotation axis.
        /// </summary>
        public Vector2 RotationDirection;

        /// <summary>
        /// The shader we use to render our planets.
        /// </summary>
        private Microsoft.Xna.Framework.Graphics.Effect _planetRenderer;

        #endregion

        #region Constructor

        public PlanetRenderer(string planetTexture, Color planetTint, float planetRadius, float planetRotationDirection, Color atmosphereTint)
            : base(planetTexture, planetTint, planetRadius)
        {
            AtmosphereTint = atmosphereTint;
            RotationDirection = Vector2.UnitX;
            Matrix matrix = Matrix.CreateRotationZ(planetRotationDirection);
            Vector2.Transform(ref RotationDirection, ref matrix, out RotationDirection);
            RotationDirection.Normalize(); // Avoid inaccuracies due to transform.
        }

        public PlanetRenderer()
            : this(string.Empty, Color.White, 0, 0, Color.White)
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

                // Get the effect, if we don't have it yet.
                if (_planetRenderer == null)
                {
                    _planetRenderer = args.Game.Content.Load<Microsoft.Xna.Framework.Graphics.Effect>("Shaders/Planet");
                }

                // Get the position at which to draw (in screen space).
                Vector2 position;
                position.X = transform.Translation.X - Scale + args.Transform.Translation.X;
                position.Y = transform.Translation.Y - Scale + args.Transform.Translation.Y;

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
                            toSun.Normalize();
                        }
                    }

                    float textureScale = _texture.Width / (3 * Scale);
                    float textureOffset = (float)args.GameTime.TotalGameTime.TotalSeconds * 10;
                    var spin = Entity.GetComponent<Spin>();
                    if (spin != null)
                    {
                        textureOffset *= spin.Value;
                    }

                    // Draw whatever is visible.
                    _planetRenderer.Parameters["DisplaySize"].SetValue(Scale);
                    _planetRenderer.Parameters["TextureSize"].SetValue(_texture.Width);
                    _planetRenderer.Parameters["TextureOffset"].SetValue(RotationDirection * textureOffset * textureScale);
                    _planetRenderer.Parameters["TextureScale"].SetValue(textureScale);
                    _planetRenderer.Parameters["PlanetTint"].SetValue(Tint.ToVector4());
                    _planetRenderer.Parameters["AtmosphereTint"].SetValue(AtmosphereTint.ToVector4());
                    _planetRenderer.Parameters["LightDirection"].SetValue(toSun);

                    args.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, _planetRenderer);
                    args.SpriteBatch.Draw(_texture, position, null, Color.White, 0, Vector2.Zero, 2 * Scale / _texture.Width, SpriteEffects.None, 0);
                    args.SpriteBatch.End();
#if DEBUG
                    var sb = new System.Text.StringBuilder();
                    sb.AppendFormat("Position: {0}\n", transform.Translation);
                    sb.AppendFormat("Rotation: {0}\n", (int)MathHelper.ToDegrees(transform.Rotation));
                    sb.AppendFormat("Scale: {0}\n", Scale);
                    sb.AppendFormat("Mass: {0:f}\n", Entity.GetComponent<Gravitation>().Mass);
                    sb.AppendFormat("uvrot: {0:f}\n", (transform.Rotation + (float)System.Math.PI) / ((float)System.Math.PI * 2f));
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
            }

            return copy;
        }

        #endregion
    }
}
