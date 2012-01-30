namespace Space.Simulation.Commands
{
    public enum SpaceCommandType
    {
        /// <summary>
        /// Restores a player's profile, creating his avatar and restoring
        /// its stats, equipment and inventory. This can only be executed
        /// once per session.
        /// </summary>
        RestoreProfile,

        /// <summary>
        /// Player input used to control his ship.
        /// </summary>
        PlayerInput,
        
        /// <summary>
        /// Equip an item from the inventory.
        /// </summary>
        Equip,

        /// <summary>
        /// A debugging command issued by a player.
        /// </summary>
        ScriptCommand,

        /// <summary>
        /// Add an item to a player's inventory.
        /// </summary>
        AddItem,
    }
}
