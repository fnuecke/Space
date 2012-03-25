using Awesomium.Core;
using Microsoft.Xna.Framework.Graphics;

namespace Awesomium.Xna
{
    /// <summary>
    /// Allow rendering of Awesomium render results to a Texture2D object that
    /// can be used in an XNA context.
    /// 
    /// <para/>
    /// Original version may be found here: Found here: http://support.awesomium.com/discussions/suggestions/41-release-basic-awesomiumsharp-extensions-for-xna-40-windows
    /// Slight modifications to make it work with the current version of Awesomium were made.
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
