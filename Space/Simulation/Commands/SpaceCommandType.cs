namespace Space.Simulation.Commands
{
    internal enum SpaceCommandType
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
        /// Have a player pick up items in his vicinity.
        /// </summary>
        PickUp,

        /// <summary>
        /// Equip an item from the inventory.
        /// </summary>
        Equip,

        /// <summary>
        /// Move an item in a player's inventory.
        /// </summary>
        MoveItem,

        /// <summary>
        /// Drop an Item
        /// </summary>
        DropItem,

        /// <summary>
        /// Use an Item
        /// </summary>
        UseItem,

        /// <summary>
        /// A debugging command issued by a player.
        /// </summary>
        ScriptCommand
    }
}
