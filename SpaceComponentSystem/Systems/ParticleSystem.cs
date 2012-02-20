using Engine.ComponentSystem.Parameterizations;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury.Renderers;
using Space.ComponentSystem.Parameterizations;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Controls the particle components in a game, passing them some
    /// information about how to render themselves.
    /// </summary>
    public class ParticleSystem : TextureRenderSystem
    {
        #region Constructor
        
        public ParticleSystem(Game game, SpriteBatch spriteBatch, IGraphicsDeviceService graphics)
            : base(game, spriteBatch)
        {
            var renderer = new SpriteBatchRenderer();
            renderer.GraphicsDeviceService = graphics;
            renderer.LoadContent(game.Content);

            _drawParameterization.Renderer = renderer;
        }

        #endregion
    }
}
