using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles AI logic updates.
    /// </summary>
    public sealed class AISystem : AbstractComponentSystem<ArtificialIntelligence>
    {
        #region Logic

        /// <summary>
        /// Updates AI component behaviors.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(GameTime gameTime, long frame, ArtificialIntelligence component)
        {
            component.Update();
        }

        #endregion
    }
}
