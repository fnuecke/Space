namespace Engine.Controller
{
    /// <summary>
    /// Used in abstract TSS server and client implementations.
    /// </summary>
    internal enum TssControllerMessage
    {
        /// <summary>
        /// Normal game command, handled in base class.
        /// </summary>
        Command,

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
        /// Server tells players about a new object to insert into the simulation.
        /// </summary>
        AddGameObject,

        /// <summary>
        /// Server tells players to remove an object from the simulation.
        /// </summary>
        RemoveGameObject,

        /// <summary>
        /// Compare the hash of the leading game state at a given frame. If
        /// the client fails the check, it'll have to request a new snapshot.
        /// </summary>
        HashCheck
    }
}
