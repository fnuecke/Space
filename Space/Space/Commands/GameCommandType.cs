namespace Space.Commands
{
    enum GameCommandType
    {
        /// <summary>
        /// Server sends current frame to clients, used to synchronize
        /// run speeds of clients to server.
        /// </summary>
        Synchronize,

        /// <summary>
        /// Client requested the game state, e.g. because it could not
        /// roll back to a required state.
        /// </summary>
        GameStateRequest,

        /// <summary>
        /// Server sends game state to client in response to <c>GameStateRequest</c>.
        /// </summary>
        GameStateResponse,

        /// <summary>
        /// Player sends input command / server authorizes player command.
        /// </summary>
        PlayerInput,

        /// <summary>
        /// Server tells players about a new player spawn.
        /// </summary>
        AddPlayerShip,

        /// <summary>
        /// Server tells players about a player removal.
        /// </summary>
        RemovePlayerShip
    }
}
