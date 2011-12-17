using Engine.ComponentSystem.Parameterizations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Basic implementation of a render system.
    /// </summary>
    public class RenderSystem : AbstractComponentSystem<RendererParameterization>
    {
        /// <summary>
        /// The reusable parameterization.
        /// </summary>
        private RendererParameterization _parameterization;

        public RenderSystem(SpriteBatch spriteBatch, ContentManager contentManager)
        {
            _parameterization = new RendererParameterization(spriteBatch, contentManager);
        }

        public override void Update()
        {
            // Get translation, which may be overridden.
            _parameterization.Translation = GetTranslation();
            // Then render all components.
            foreach (var component in components)
            {
                component.Update(_parameterization);
            }
        }

        /// <summary>
        /// Override in subclasses for specific translation of the view.
        /// </summary>
        /// <returns>the translation of the view to use when rendering.</returns>
        protected virtual Vector2 GetTranslation()
        {
            return Vector2.Zero;
        }
    }
}
