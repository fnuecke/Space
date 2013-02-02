using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>Sent when the graphics device has been (re)created and assets should be loaded.</summary>
    public struct GraphicsDeviceCreated
    {
        /// <summary>The new graphics device service.</summary>
        public IGraphicsDeviceService Graphics;
    }
}