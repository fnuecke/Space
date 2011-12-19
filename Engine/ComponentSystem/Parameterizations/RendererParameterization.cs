using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Parameterizations
{
    public class RendererParameterization
    {
        public SpriteBatch SpriteBatch { get; private set; }

        public ContentManager Content { get; private set; }

        public Vector2 Translation { get; set; }

        public RendererParameterization(SpriteBatch spriteBatch, ContentManager contentManager)
        {
            this.SpriteBatch = spriteBatch;
            this.Content = contentManager;
        }
    }
}
