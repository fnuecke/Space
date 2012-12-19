using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Part of image post processing, this system uses the rendered image
    /// taken from the render target set up in the <see cref="PostProcessingPreRenderSystem"/>
    /// to apply post processing effects.
    /// 
    /// This system should run after all other render systems.
    /// </summary>
    public sealed class PostProcessingPostRenderSystem : AbstractSystem, IDrawingSystem, IMessagingSystem
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
        /// The sprite batch we will render the final output to.
        /// </summary>
        private SpriteBatch _spriteBatch;

        #endregion

        #region Logic

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

        /// <summary>
        /// Draws the system.
        /// </summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Reset our graphics device (pop our off-screen render target).
            _spriteBatch.GraphicsDevice.SetRenderTarget(null);

            // Dump everything we rendered into our buffer to the screen.
            var preprocessor = (PostProcessingPreRenderSystem)Manager.GetSystem(PostProcessingPreRenderSystem.TypeId);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _spriteBatch.Draw(preprocessor.RenderTarget, _spriteBatch.GraphicsDevice.PresentationParameters.Bounds, Color.White);
            _spriteBatch.End();
        }

        #endregion
    }
}
