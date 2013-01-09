using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>Sent when the graphics device has been (re)created and assets should be loaded.</summary>
    public struct GraphicsDeviceCreated
    {
        /// <summary>A content manager that may be used to load assets.</summary>
        public ContentManager Content;

        /// <summary>The new graphics device service.</summary>
        public IGraphicsDeviceService Graphics;
    }
}