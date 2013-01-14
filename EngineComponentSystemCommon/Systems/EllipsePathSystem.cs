using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Systems
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
            // Get the center, the position of the entity we're rotating around.
            var center = ((Transform) Manager.GetComponent(component.CenterEntityId, Transform.TypeId)).Translation;

            // Get the angle based on the time passed.
            var t = component.PeriodOffset + MathHelper.Pi * frame / component.Period;
            var sinT = (float) System.Math.Sin(t);
            var cosT = (float) System.Math.Cos(t);

            // Compute the current position and set it.
            var transform = ((Transform) Manager.GetComponent(component.Entity, Transform.TypeId));
            transform.Translation = new FarPosition(
                center.X + component.PrecomputedA + component.PrecomputedB * cosT - component.PrecomputedC * sinT,
                center.Y + component.PrecomputedD + component.PrecomputedE * cosT + component.PrecomputedF * sinT);
        }

        #endregion
    }
}