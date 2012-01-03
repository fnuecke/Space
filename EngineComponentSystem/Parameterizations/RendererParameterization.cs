using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
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
    public class RendererParameterization
    {
        /// <summary>
        /// The sprite batch object to use for rendering into.
        /// </summary>
        public SpriteBatch SpriteBatch;

        /// <summary>
        /// The content manager to retrieve data required to render from.
        /// </summary>
        public ContentManager Content;

        /// <summary>
        /// The transform to apply when rendering, in addition to any specific
        /// translations that might apply.
        /// </summary>
        public Matrix Transform;
    }
}
