using System;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Messages;
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
    public sealed class GraphicsDeviceSystem : AbstractSystem, IDisposable
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>The graphics device service used to keep track of our graphics device.</summary>
        public IGraphicsDeviceService Graphics
        {
            get { return _graphics; }
        }

        #endregion

        #region Fields

        /// <summary>The graphics device service used to keep track of our graphics device.</summary>
        private readonly IGraphicsDeviceService _graphics;

        /// <summary>Whether the system has been initialized, i.e. the first 'device created' message has been sent.</summary>
        private bool _initialized;

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
                if (Manager != null)
                {
                    GraphicsDeviceDisposing message;
                    Manager.SendMessage(message);
                }
            }
        }

        #endregion

        #region Logic

        /// <summary>Draws the system.</summary>
        [MessageCallback]
        public void OnDraw(Draw message)
        {
            if (_initialized)
            {
                return;
            }

            // Trigger loading for the first time, then disable self.
            GraphicsDeviceCreated created;
            created.Graphics = _graphics;
            Manager.SendMessage(created);

            _initialized = true;
        }

        private void GraphicsOnDeviceCreated(object sender, EventArgs eventArgs)
        {
            if (Manager != null)
            {
                GraphicsDeviceCreated message;
                message.Graphics = _graphics;
                Manager.SendMessage(message);
            }
        }

        private void GraphicsOnDeviceDisposing(object sender, EventArgs eventArgs)
        {
            if (Manager != null)
            {
                GraphicsDeviceDisposing message;
                Manager.SendMessage(message);
            }
        }

        private void GraphicsOnDeviceReset(object sender, EventArgs eventArgs)
        {
            if (Manager != null)
            {
                GraphicsDeviceReset message;
                message.Graphics = _graphics;
                Manager.SendMessage(message);
            }
        }

        #endregion
    }
}