using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury.Renderers;
using Space.ComponentSystem.Parameterizations;

namespace Space.ComponentSystem.Systems
{
    public class ParticleSystem : AbstractComponentSystem<ParticleParameterization>
    {
        #region Fields
        
        /// <summary>
        /// Reusable parameterization object.
        /// </summary>
        private ParticleParameterization _parameterization = new ParticleParameterization();

        #endregion

        #region Constructor
        
        public ParticleSystem(ContentManager content, IGraphicsDeviceService graphics)
        {
            var renderer = new SpriteBatchRenderer();
            renderer.GraphicsDeviceService = graphics;
            renderer.LoadContent(content);

            _parameterization.Content = content;
            _parameterization.Renderer = renderer;
            _parameterization.Matrix = Matrix.Identity;
        }

        #endregion

        #region Logic

        public override void Update(ComponentSystemUpdateType updateType, long frame)
        {
            _parameterization.UpdateType = updateType;
            if (updateType == ComponentSystemUpdateType.Logic)
            {
                // Logic update, check if new particles should be triggered.
                foreach (var component in Components)
                {
                    UpdateComponent(component, _parameterization);
                }
            }
            else if (updateType == ComponentSystemUpdateType.Display)
            {
                // Display update, draw all our particles.
                _parameterization.Matrix.Translation = GetTranslation();
                foreach (var component in Components)
                {
                    UpdateComponent(component, _parameterization);
                }
            }
        }

        /// <summary>
        /// Override in subclasses for specific translation of the view.
        /// </summary>
        /// <returns>the translation of the view to use when rendering.</returns>
        protected virtual Vector3 GetTranslation()
        {
            return Vector3.Zero;
        }

        #endregion
    }
}
