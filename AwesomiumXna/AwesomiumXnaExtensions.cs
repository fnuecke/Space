using Awesomium.Core;
using Microsoft.Xna.Framework.Graphics;

namespace Awesomium.Xna
{
    /// <summary>
    /// Allow rendering of Awesomium render results to a Texture2D object that
    /// can be used in an XNA context.
    /// </summary>
    public static class AwesomiumXnaExtensions
    {
        /// <summary>
        /// Renders the Awesomium buffer to the specified texture.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="texture">The texture.</param>
        /// <returns>The specified texture.</returns>
        public static Texture2D RenderTexture2D(this RenderBuffer buffer, Texture2D texture)
        {
            TextureFormatConverter.DirectBlit(buffer, ref texture);
            return texture;
        }
    }
}
