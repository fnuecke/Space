using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Basic implementation of a render system. This is kept generic to allow
    /// sub-classing with extended rendering configurations (see the particle
    /// render system).
    /// </summary>
    public class RenderSystem<TRendererParameterization>
        : AbstractComponentSystem<NullParameterization, TRendererParameterization>
        where TRendererParameterization : RendererParameterization, new()
    {
        #region Fields

        /// <summary>
        /// The reusable parameterization.
        /// </summary>
        protected TRendererParameterization _parameterization;

        #endregion

        #region Constructor
        
        public RenderSystem(SpriteBatch spriteBatch, ContentManager contentManager)
        {
            _parameterization = new TRendererParameterization();
            _parameterization.SpriteBatch = spriteBatch;
            _parameterization.Content = contentManager;
            _parameterization.Transform = Matrix.Identity;
        }

        #endregion

        #region Logic

        public override void Draw(long frame)
        {
            // Get translation, which may be overridden.
            _parameterization.Transform.Translation = GetTranslation();
            // Then render all components.
            _parameterization.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            foreach (var component in DrawableComponents)
            {
                if (component.Enabled)
                {
                    component.Draw(_parameterization);
                }
            }
            _parameterization.SpriteBatch.End();
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
