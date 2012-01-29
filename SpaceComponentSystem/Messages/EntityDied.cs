namespace Space.ComponentSystem.Messages
{
    /// <summary>
    /// Fired by the <c>Health</c> component when health reaches zero. This is
    /// only broadcast locally (i.e. in entity scope). To check if an entity
    /// was completely destroyed (removed) listen to <c>EntityRemoved</c>
    /// messages. To check if an entity was temporarily destroy, check the
    /// health of the entity, or use the <c>ShipInfo</c>'s <c>IsAlive</c>
    /// property.
    /// </summary>
    public struct EntityDied
    {
    }
}
