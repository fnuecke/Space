namespace Space.Simulation.Commands
{
    public enum SpaceCommandType
    {
        /// <summary>
        /// Player sends input command / server authorizes player command.
        /// </summary>
        PlayerInput,

        /// <summary>
        /// A debugging command issued by a player.
        /// </summary>
        ScriptCommand
    }
}
