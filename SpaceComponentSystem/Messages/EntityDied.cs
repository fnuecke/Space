namespace Space.ComponentSystem.Messages
{
    /// <summary>
    /// Fired by the <c>Health</c> component when health reaches zero. To
    /// check if an entity was completely destroyed (removed) check for
    /// <c>EntityRemoved</c> messages.
    /// </summary>
    public struct EntityDied
    {
        /// <summary>
        /// The entity that just died.
        /// </summary>
        public int KilledEntity;

        /// <summary>
        /// The entity that triggered the death of the other entity.
        /// </summary>
        public int KillingEntity;
    }
}
