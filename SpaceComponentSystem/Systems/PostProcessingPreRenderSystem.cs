using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    ///     Part of image post processing, this system sets up the render target to capture the original rendered image to be
    ///     processed in the <see cref="PostProcessingPostRenderSystem"/>. This system should run before any other render
    ///     systems.
    /// </summary>
    public sealed class PostProcessingPreRenderSystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>Gets the render target used for capturing the rendered image.</summary>
        public Texture2D RenderTarget
        {
            get { return _scene; }
        }

        #endregion

        #region Fields

        /// <summary>The render target we use to render the scene to.</summary>
        private RenderTarget2D _scene;

        #endregion

        #region Logic
        
        /// <summary>Draws the system.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Set our custom render target to render everything into an
            // off-screen texture, first.
            _scene.GraphicsDevice.SetRenderTarget(_scene);
            _scene.GraphicsDevice.Clear(Color.DarkSlateGray);
        }

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            Manager.AddMessageListener<GraphicsDeviceCreated>(OnGraphicsDeviceCreated);
            Manager.AddMessageListener<GraphicsDeviceDisposing>(OnGraphicsDeviceDisposing);
        }

        private void OnGraphicsDeviceCreated(GraphicsDeviceCreated message)
        {
            var pp = message.Graphics.GraphicsDevice.PresentationParameters;
            _scene = new RenderTarget2D(
                message.Graphics.GraphicsDevice,
                pp.BackBufferWidth,
                pp.BackBufferHeight,
                false,
                pp.BackBufferFormat,
                DepthFormat.None,
                pp.MultiSampleCount,
                RenderTargetUsage.PreserveContents);
        }

        private void OnGraphicsDeviceDisposing(GraphicsDeviceDisposing message)
        {
            if (_scene != null)
            {
                _scene.Dispose();
                _scene = null;
            }
        }

        #endregion
    }
}