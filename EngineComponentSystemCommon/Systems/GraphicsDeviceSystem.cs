using System;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    ///     This system keeps track of our graphics device, and sends messages in case unloading / reloading if assets is
    ///     required.
    /// </summary>
    [Packetizable(false)]
    public sealed class GraphicsDeviceSystem : AbstractSystem, IDrawingSystem
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Determines whether this system is enabled, i.e. whether it should draw.</summary>
        /// <value>
        ///     <c>true</c> if this instance is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>The graphics device service used to keep track of our graphics device.</summary>
        public IGraphicsDeviceService Graphics
        {
            get { return _graphics; }
        }

        #endregion

        #region Fields

        /// <summary>The graphics device service used to keep track of our graphics device.</summary>
        private readonly IGraphicsDeviceService _graphics;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="GraphicsDeviceSystem"/> class.
        /// </summary>
        /// <param name="graphics">The graphics device service.</param>
        public GraphicsDeviceSystem(IGraphicsDeviceService graphics)
        {
            _graphics = graphics;

            graphics.DeviceCreated += GraphicsOnDeviceCreated;
            graphics.DeviceDisposing += GraphicsOnDeviceDisposing;
            graphics.DeviceReset += GraphicsOnDeviceReset;
        }

        /// <summary>
        ///     Releases unmanaged resources and performs other cleanup operations before the
        ///     <see cref="AbstractSystem"/> is reclaimed by garbage collection.
        /// </summary>
        ~GraphicsDeviceSystem()
        {
            Dispose(false);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources;
        ///     <c>false</c> to release only unmanaged resources.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _graphics.DeviceCreated -= GraphicsOnDeviceCreated;
                _graphics.DeviceDisposing -= GraphicsOnDeviceDisposing;
                _graphics.DeviceReset -= GraphicsOnDeviceReset;
                GraphicsDeviceDisposing message;
                Manager.SendMessage(message);
            }
        }

        #endregion

        #region Logic

        /// <summary>Draws the system.</summary>
        /// <param name="frame">The frame that should be rendered.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public void Draw(long frame, float elapsedMilliseconds)
        {
            // Trigger loading for the first time, then disable self.
            GraphicsDeviceCreated message;
            message.Graphics = _graphics;
            Manager.SendMessage(message);

            Enabled = false;
        }

        private void GraphicsOnDeviceCreated(object sender, EventArgs eventArgs)
        {
            GraphicsDeviceCreated message;
            message.Graphics = _graphics;
            Manager.SendMessage(message);
        }

        private void GraphicsOnDeviceDisposing(object sender, EventArgs eventArgs)
        {
            GraphicsDeviceDisposing message;
            Manager.SendMessage(message);
        }

        private void GraphicsOnDeviceReset(object sender, EventArgs eventArgs)
        {
            GraphicsDeviceReset message;
            message.Graphics = _graphics;
            Manager.SendMessage(message);
        }

        #endregion
    }
}