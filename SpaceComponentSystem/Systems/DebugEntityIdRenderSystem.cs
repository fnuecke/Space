using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Renders entity ids at their position, if they have a position.
    /// </summary>
    public sealed class DebugEntityIdRenderSystem : AbstractSystem, IDrawingSystem, IMessagingSystem
    {
        #region Properties

        /// <summary>
        /// Determines whether this system is enabled, i.e. whether it should draw.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// The spritebatch to use for rendering.
        /// </summary>
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// The font to use for rendering.
        /// </summary>
        private SpriteFont _font;

        #endregion

        #region Logic

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            var camera = (CameraSystem)Manager.GetSystem(CameraSystem.TypeId);

            // Get camera transform.
            var cameraTransform = camera.Transform;
            var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);

            // Iterate over all visible entities.
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, cameraTransform.Matrix);
            foreach (var entity in camera.VisibleEntities)
            {
                var transform = (Transform)Manager.GetComponent(entity, Transform.TypeId);
                if (transform != null)
                {
                    FarPosition position;
                    interpolation.GetInterpolatedPosition(transform.Entity, out position);
                    position += cameraTransform.Translation;
                    _spriteBatch.DrawString(_font, "ID: " + transform.Entity, (Vector2)position, Color.White, 0,
                                            Vector2.Zero, 1f / camera.CameraZoom, SpriteEffects.None, 1);
                }
            }
            _spriteBatch.End();
        }

        /// <summary>
        /// Handle a message of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(T message) where T : struct
        {
            {
                var cm = message as GraphicsDeviceCreated?;
                if (cm != null)
                {
                    _spriteBatch = new SpriteBatch(cm.Value.Graphics.GraphicsDevice);
                    _font = cm.Value.Content.Load<SpriteFont>("Fonts/ConsoleFont");
                }
            }
            {
                var cm = message as GraphicsDeviceDisposing?;
                if (cm != null)
                {
                    if (_spriteBatch != null)
                    {
                        _spriteBatch.Dispose();
                        _spriteBatch = null;
                    }
                }
            }
        }

        #endregion
    }
}
