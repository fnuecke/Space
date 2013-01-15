using Engine.ComponentSystem.Spatial.Components;
using Engine.ComponentSystem.Systems;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Spatial.Systems
{
    /// <summary>Applies a component's friction to its velocity.</summary>
    public sealed class FrictionSystem : AbstractParallelComponentSystem<Friction>
    {
        /// <summary>Updates the component's entity by adjusting it's velocity.</summary>
        /// <param name="frame">The current frame.</param>
        /// <param name="component">The component.</param>
        protected override void UpdateComponent(long frame, Friction component)
        {
            // Apply friction only if set to a positive value.
            if (component.Value <= 0)
            {
                return;
            }

            // Get velocity.
            var velocity = ((Velocity) Manager.GetComponent(component.Entity, Velocity.TypeId));

            // Save previous velocity for stop check (due to MinVelocity).
            var previousVelocity = velocity.LinearVelocity.LengthSquared();

            // Apply friction.
            velocity.LinearVelocity *= (1.0f - component.Value);

            // If we're below a certain minimum speed, just stop, otherwise
            // it'd be hard to. We only stop if we were faster than the minimum,
            // before application of friction. Otherwise we might have problems
            // getting moving at all, if the acceleration is too low.
            if (previousVelocity >= component.StopVelocity &&
                velocity.LinearVelocity.LengthSquared() < component.StopVelocity)
            {
                velocity.LinearVelocity = Vector2.Zero;
            }
        }
    }
}