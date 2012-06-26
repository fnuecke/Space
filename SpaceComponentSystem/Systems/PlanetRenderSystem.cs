using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Renders planets.
    /// </summary>
    public sealed class PlanetRenderSystem : AbstractComponentSystem<PlanetRenderer>
    {
        #region Fields

        /// <summary>
        /// The renderer we use to render our planet.
        /// </summary>
        private static Planet _planet;

        /// <summary>
        /// The content manager used to load textures.
        /// </summary>
        private readonly ContentManager _content;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetRenderSystem"/> class.
        /// </summary>
        /// <param name="game">The game the system belongs to.</param>
        public PlanetRenderSystem(Game game)
        {
            _content = game.Content;
            if (_planet == null)
            {
                _planet = new Planet(game);
            }
        }

        #endregion

        #region Logic

        /// <summary>
        /// Load surface texture if necessary.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(GameTime gameTime, long frame, PlanetRenderer component)
        {
            if (component.Texture == null)
            {
                component.Texture = _content.Load<Texture2D>(component.TextureName);
            }
        }

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void DrawComponent(GameTime gameTime, long frame, PlanetRenderer component)
        {
            // The position and orientation we're rendering at and in.
            var transform = Manager.GetComponent<Transform>(component.Entity);
            var translation = Manager.GetSystem<CameraSystem>().GetTranslation();

            // Get zoom from camera.
            var zoom = Manager.GetSystem<CameraSystem>().Zoom;

            // Get the position at which to draw (in screen space).
            Vector2 position;
            position.X = transform.Translation.X + translation.X;
            position.Y = transform.Translation.Y + translation.Y;

            // Check if we're even visible.
            var clipRectangle = _planet.GraphicsDevice.Viewport.Bounds;
            // Inflate clip by zoomed amount and object radius.
            clipRectangle.Inflate((int)(clipRectangle.Width / zoom - clipRectangle.Width + 2 * component.Radius),
                                  (int)(clipRectangle.Height / zoom - clipRectangle.Height + 2 * component.Radius));
            if (clipRectangle.Contains((int)position.X, (int)position.Y)) 
            {
                // Get position relative to our sun, to rotate atmosphere and shadow.
                var toSun = Vector2.Zero;
                int sun = GetSun(component.Entity);
                if (sun > 0)
                {
                    var sunTransform = Manager.GetComponent<Transform>(sun);
                    if (sunTransform != null)
                    {
                        toSun = sunTransform.Translation - transform.Translation;
                        var matrix = Matrix.CreateRotationZ(-transform.Rotation);
                        Vector2.Transform(ref toSun, ref matrix, out toSun);
                        toSun.Normalize();
                    }
                }

                // Set parameters and draw.
                _planet.SetCenter(position);
                _planet.SetRotation(transform.Rotation);
                _planet.SetSize(component.Radius * 2);
                _planet.SetSurfaceTexture(component.Texture);
                _planet.SetSurfaceTint(component.PlanetTint);
                _planet.SetAtmosphereTint(component.AtmosphereTint);
                _planet.SetLightDirection(toSun);
                _planet.SetGameTime(gameTime);
                _planet.SetScale(zoom);
                _planet.Draw();
            }
        }

        /// <summary>
        /// Utility method to find the sun we're rotating around.
        /// </summary>
        /// <returns></returns>
        private int GetSun(int entity)
        {
            int sun = 0;
            var ellipse = Manager.GetComponent<EllipsePath>(entity);
            while (ellipse != null)
            {
                sun = ellipse.CenterEntityId;
                ellipse = Manager.GetComponent<EllipsePath>(sun);
            }
            return sun;
        }

        #endregion
    }
}
