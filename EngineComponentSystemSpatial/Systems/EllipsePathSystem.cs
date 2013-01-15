using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Spatial.Systems
{
    /// <summary>Makes an entity move along an ellipsoid path.</summary>
    public sealed class EllipsePathSystem : AbstractParallelComponentSystem<EllipsePath>
    {
        #region Logic

        /// <summary>Updates the component by adjusting it's position for the specified frame.</summary>
        /// <param name="frame">The current frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, EllipsePath component)
        {
            component.Update(frame);
        }

        #endregion
    }
}