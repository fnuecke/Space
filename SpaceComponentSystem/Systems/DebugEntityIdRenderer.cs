using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
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
            var renderer = (TextureRenderSystem)Manager.GetSystem(TextureRenderSystem.TypeId);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, transform.Matrix);
            foreach (var component in Components)
            {
                if (view.Contains(component.Translation))
                {
                    FarPosition position;
                    if (!renderer.GetInterpolatedPosition(component.Entity, out position))
                    {
                        position = component.Translation;
                    }
                    position += transform.Translation;
                    _spriteBatch.DrawString(_font, "ID: " + component.Entity, (Vector2)position, Color.White);
                }
            }
            _spriteBatch.End();
        }

        #endregion
    }
}
