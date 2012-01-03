using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury.Renderers;
using Space.ComponentSystem.Parameterizations;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Controls the particle components in a game, passing them some
    /// information about how to render themselves.
    /// </summary>
    public class ParticleSystem : RenderSystem<ParticleParameterization>
    {
        #region Constructor
        
        public ParticleSystem(SpriteBatch spriteBatch, ContentManager content, IGraphicsDeviceService graphics)
            : base(spriteBatch, content)
        {
            var renderer = new SpriteBatchRenderer();
            renderer.GraphicsDeviceService = graphics;
            renderer.LoadContent(content);

            _parameterization.Renderer = renderer;
        }

        #endregion
    }
}
