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
            // Get the center, the position of the entity we're rotating around.
            Vector2 center = Manager.GetComponent<Transform>(component.CenterEntityId).Translation;

            // Get the angle based on the time passed.
            float t = component.PeriodOffset + MathHelper.Pi * frame / component.Period;
            float sinT = (float)System.Math.Sin(t);
            float cosT = (float)System.Math.Cos(t);

            // Compute the current position and set it.
            Manager.GetComponent<Transform>(component.Entity).SetTranslation(
                center.X + component.precomputedA + component.precomputedB * cosT - component.precomputedC * sinT,
                center.Y + component.precomputedD + component.precomputedE * cosT + component.precomputedF * sinT
            );
        }
    }
}
