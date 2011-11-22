using Engine.Serialization;

namespace Engine.Session
{
    public delegate void GameInfoRequestedEventHandler(out Packet data);
    public delegate void JoinRequestedEventHandler(out Packet data);

    public interface IServerSession : ISession
    {

        /// <summary>
        /// Called when an unconnected client requests game info.
        /// 
        /// IMPORTANT: this may be called asynchronously!
        /// </summary>
        event GameInfoRequestedEventHandler GameInfoRequested;

        /// <summary>
        /// A player is joining the game. Fill in any arbitrary data to send
        /// back to the joining client here.
        /// </summary>
        event JoinRequestedEventHandler JoinRequested;

        /// <summary>
        /// Kick a player from the session.
        /// </summary>
        /// <param name="player">the number of the player to kick.</param>
        void Kick(int player);

    }
}
