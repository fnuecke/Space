using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.ComponentSystem.Components;
using Space.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Renders suns.
    /// </summary>
    public sealed class SunRenderSystem : AbstractComponentSystem<SunRenderer>
    {
        #region Fields

        /// <summary>
        /// The sun renderer we use.
        /// </summary>
        private static Sun _sun;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SunRenderSystem"/> class.
        /// </summary>
        /// <param name="game">The game to create the system for.</param>
        /// <param name="spriteBatch">The sprite batch to use for rendering.</param>
        public SunRenderSystem(Game game, SpriteBatch spriteBatch)
        {
            if (_sun == null)
            {
              //  var cam = Manager.GetSystem<CameraSystem>();
                _sun = new Sun(game);
                _sun.LoadContent(spriteBatch, game.Content);
            }
            
        }

        #endregion

        #region Logic

        /// <summary>
        /// Draws the component.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void DrawComponent(GameTime gameTime, long frame, SunRenderer component)
        {
            // The position and orientation we're rendering at and in.
            var transform = Manager.GetComponent<Transform>(component.Entity);
            var translation = Manager.GetSystem<CameraSystem>().GetTranslation();

            // Check if we need to draw (in bounds of view port). Use a
            // large bounding rectangle to account for the glow, so that
            // doesn't suddenly pop up.
            Rectangle sunBounds;
            sunBounds.Width = (int)(component.Radius * 4);
            sunBounds.Height = (int)(component.Radius * 4);
            sunBounds.X = (int)(transform.Translation.X - component.Radius + translation.X);
            sunBounds.Y = (int)(transform.Translation.Y - component.Radius + translation.Y);

            var zoom = Manager.GetSystem<CameraSystem>().Zoom;
            var screenBounds = _sun.GraphicsDevice.Viewport.Bounds;
            //Inflate bounds by zoomed amount
            screenBounds.Inflate((int)(screenBounds.Width / zoom - screenBounds.Width), (int)(screenBounds.Height / zoom - screenBounds.Height));
            if (sunBounds.Intersects(screenBounds))
            {
                _sun.SetGameTime(gameTime);
                _sun.SetSize(component.Radius * 2);
                _sun.SetCenter(transform.Translation.X + translation.X,
                               transform.Translation.Y + translation.Y);
                _sun.Scale = zoom;
                _sun.Draw();
            }
        }

        #endregion
    }
}
