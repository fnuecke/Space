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

        #region Logic

        /// <summary>
        /// We also call update with our parameterization, to allow effects
        /// loading their info, to avoid visual glitches (particles being
        /// reset when rolling back).
        /// </summary>
        /// <param name="frame"></param>
        public override void Update(long frame)
        {
            foreach (var component in UpdateableComponents)
            {
                if (component.Enabled)
                {
                    component.Update(_parameterization);
                }
            }
        }

        #endregion
    }
}
