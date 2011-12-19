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
        #region Fields

        /// <summary>
        /// The reusable parameterization.
        /// </summary>
        protected RendererParameterization parameterization;

        #endregion

        #region Constructor
        
        public RenderSystem(SpriteBatch spriteBatch, ContentManager contentManager)
        {
            parameterization = new RendererParameterization(spriteBatch, contentManager);
        }

        #endregion

        #region Logic
        
        public override void Update(ComponentSystemUpdateType updateType)
        {
            if (updateType != ComponentSystemUpdateType.Display)
            {
                return;
            }

            // Get translation, which may be overridden.
            parameterization.Translation = GetTranslation();
            // Then render all components.
            foreach (var component in Components)
            {
                component.Update(parameterization);
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

        #endregion
    }
}
