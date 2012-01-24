using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Parameterizations
{
    /// <summary>
    /// Identifies components to be added to a <c>RenderSystem</c>.
    /// 
    /// <para>
    /// This will provide the components with some context to render into
    /// in their <c>Update</c> call.
    /// </para>
    /// </summary>
    public class RendererUpdateParameterization
    {
        /// <summary>
        /// The game we're running.
        /// </summary>
        public Game Game;

        /// <summary>
        /// The sprite batch object to use for rendering into.
        /// </summary>
        public SpriteBatch SpriteBatch;

        /// <summary>
        /// The current simulation frame.
        /// </summary>
        public long Frame;
    }
}
