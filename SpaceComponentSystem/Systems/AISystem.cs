using Engine.ComponentSystem.Systems;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Systems
{
    /// <summary>
    /// Handles AI logic updates.
    /// </summary>
    public sealed class AISystem : AbstractParallelComponentSystem<ArtificialIntelligence>
    {
        #region Logic

        /// <summary>
        /// Updates AI component behaviors.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, ArtificialIntelligence component)
        {
            component.Update();
        }

        #endregion
    }
}
