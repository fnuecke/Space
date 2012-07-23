﻿using Engine.ComponentSystem.Common.Components;
using Engine.ComponentSystem.Systems;

namespace Engine.ComponentSystem.Common.Systems
{
    /// <summary>
    /// Handles expiring components by removing them from the simulation when
    /// they expire.
    /// </summary>
    public sealed class ExpirationSystem : AbstractComponentSystem<Expiration>
    {
        protected override void UpdateComponent(long frame, Expiration component)
        {
            if (component.TimeToLive > 0)
            {
                --component.TimeToLive;
            }
            else
            {
                Manager.RemoveEntity(component.Entity);
            }
        }
    }
}
