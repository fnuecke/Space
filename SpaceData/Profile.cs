namespace Space.Data
{
    /// <summary>
    /// Represents a player profile.
    /// </summary>
    public sealed class Profile
    {
        /// <summary>
        /// The name of the player.
        /// </summary>
        public string Name;

        /// <summary>
        /// The player's current ship with equipment.
        /// </summary>
        public ShipData Ship;
    }
}
