using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Parameterizations
{
    public class DrawParameterization
    {
        public SpriteBatch SpriteBatch { get; private set; }

        public ContentManager Content { get; private set; }
    }
}
