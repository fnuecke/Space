namespace Space.ComponentSystem.Messages
{
    /// <summary>
    /// Fired when a ship completely blocks damage.
    /// </summary>
    public struct DamageBlocked
    {
        /// <summary>
        /// The entity that blocked the damage.
        /// </summary>
        public int Entity;
    }
}
