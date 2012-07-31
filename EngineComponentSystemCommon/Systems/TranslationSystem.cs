using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// Updates all components' translation. This is done in one synchronous step
    /// to simplify the way a lot of the other systems work (so they can "change"
    /// the translation without interfering with others reading the translation).
    /// </summary>
    public sealed class TranslationSystem : AbstractUpdatingComponentSystem<Transform>
    {
        /// <summary>
        /// Updates the component's translation, if necessary. This will trigger
        /// a TranslationChanged message in that case.
        /// </summary>
        /// <param name="frame">The current simulation frame.</param>
        /// <param name="component">The component to update.</param>
        protected override void UpdateComponent(long frame, Transform component)
        {
            component.ApplyTranslation();
        }
    }
}
