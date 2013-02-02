using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>Sent when the graphics device had to be reset.</summary>
    public struct GraphicsDeviceReset
    {
        /// <summary>The new graphics device service.</summary>
        public IGraphicsDeviceService Graphics;
    }
}