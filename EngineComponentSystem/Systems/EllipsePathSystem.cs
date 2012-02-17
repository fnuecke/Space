using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Makes an entity move along an ellipsoid path.
    /// </summary>
    public sealed class EllipsePathSystem : AbstractComponentSystem<EllipsePath>
    {
        protected override void UpdateComponent(GameTime gameTime, long frame, EllipsePath component)
        {
            // Try to get the center of the entity we're rotating around.
            Vector2 center = Vector2.Zero;
            var centerEntity = component.Entity.Manager.GetEntity(component.CenterEntityId);
            if (centerEntity != null)
            {
                center = centerEntity.GetComponent<Transform>().Translation;
            }

            // Get the angle based on the time passed.
            float t = component.PeriodOffset + MathHelper.Pi * frame / component.Period;
            float sinT = (float)AbstractSystem.Math.Sin(t);
            float cosT = (float)AbstractSystem.Math.Cos(t);

            // Compute the current position and set it.
            component.Entity.GetComponent<Transform>().SetTranslation(
                center.X + component.precomputedA + component.precomputedB * cosT - component.precomputedC * sinT,
                center.Y + component.precomputedD + component.precomputedE * cosT + component.precomputedF * sinT
            );
        }
    }
}
