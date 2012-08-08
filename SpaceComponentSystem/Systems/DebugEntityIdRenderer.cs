using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Renders entity ids at their position, if they have a position.
    /// </summary>
    public sealed class DebugEntityIdRenderer : AbstractComponentSystem<Transform>, IDrawingSystem
    {
        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The spritebatch to use for rendering.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        /// <summary>
        /// The font to use for rendering.
        /// </summary>
        private readonly SpriteFont _font;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugEntityIdRenderer"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        public DebugEntityIdRenderer(ContentManager content, SpriteBatch spriteBatch)
        {
            _spriteBatch = spriteBatch;
            _font = content.Load<SpriteFont>("Fonts/ConsoleFont");
        }

        #endregion

        #region Logic

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        public void Draw(long frame)
        {
            var camera = ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId));

            // Get all renderable entities in the viewport.
            var view = camera.ComputeVisibleBounds(_spriteBatch.GraphicsDevice.Viewport);

            // Get camera transform.
            var transform = camera.Transform;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, transform.Matrix);
            foreach (var component in Components)
            {
                if (view.Contains(component.Translation))
                {
                    var position = (Vector2)(component.Translation + transform.Translation);
                    _spriteBatch.DrawString(_font, "ID: " + component.Entity, position, Color.White);
                }
            }
            _spriteBatch.End();
        }

        #endregion
    }
}
