﻿using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Systems
{
    /// <summary>
    /// Applies a component's velocity to its transform.
    /// </summary>
    public sealed class VelocitySystem : AbstractComponentSystem<Velocity>
    {
        protected override void UpdateComponent(GameTime gameTime, long frame, Velocity component)
        {
            Manager.GetComponent<Transform>(component.Entity).AddTranslation(ref component.Value);
        }
    }
}