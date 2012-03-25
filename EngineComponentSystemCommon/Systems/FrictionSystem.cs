using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Applies a component's friction to its velocity.
    /// </summary>
    public sealed class FrictionSystem : AbstractComponentSystem<Friction>
    {
        protected override void UpdateComponent(GameTime gameTime, long frame, Friction component)
        {
            // Apply friction only if set to a positive value.
            if (component.Value > 0)
            {
                // Get velocity.
                var velocity = Manager.GetComponent<Velocity>(component.Entity);

                // Save previous velocity for stop check (due to MinVelocity).
                var previousVelocity = velocity.Value.LengthSquared();

                // Apply friction.
                //velocity.Value = velocity.Value * (1 - Value);
                velocity.Value.X *= (1 - component.Value);
                velocity.Value.Y *= (1 - component.Value);

                // If we're below a certain minimum speed, just stop, otherwise
                // it'd be hard to. We only stop if we were faster than the minimum,
                // before application of friction. Otherwise we might have problems
                // getting moving at all, if the acceleration is too low.
                if (previousVelocity >= component.StopVelocity &&
                    velocity.Value.LengthSquared() < component.StopVelocity)
                {
                    velocity.Value = Vector2.Zero;
                }
            }
        }
    }
}
