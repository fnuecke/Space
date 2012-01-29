namespace Space.Simulation.Commands
{
    public enum SpaceCommandType
    {
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
        AddItem
    }
}
