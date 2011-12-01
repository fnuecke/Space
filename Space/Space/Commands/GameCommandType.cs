namespace Space.Commands
{
    enum GameCommandType
    {
        /// <summary>
        /// Player sends input command / server authorizes player command.
        /// </summary>
        PlayerInput,

        /// <summary>
        /// Player's data has changed somehow.
        /// </summary>
        PlayerDataChanged
    }
}
