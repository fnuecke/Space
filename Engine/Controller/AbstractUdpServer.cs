using System;
using Engine.Serialization;
using Engine.Session;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for game servers using the UDP network protocol.
    /// </summary>
    public abstract class AbstractUdpServer<TCommandType, TPlayerData, TPacketizerContext> : AbstractUdpController<IServerSession<TPlayerData, TPacketizerContext>, TCommandType, TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPacketizerContext>, new()
        where TCommandType : struct
    {
        #region Construction / Destruction

        /// <summary>
        /// Initializes the session and base classes.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="maxPlayers">the number of allowed players in the game.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractUdpServer(Game game, int maxPlayers, ushort port, string header)
            : base(game, port, header)
        {
            Session = SessionFactory.StartServer<TPlayerData, TPacketizerContext>(game, protocol, maxPlayers);
        }

        /// <summary>
        /// Attach ourselves as listeners.
        /// </summary>
        public override void Initialize()
        {
            Session.GameInfoRequested += HandleGameInfoRequested;
            Session.JoinRequested += HandleJoinRequested;

            base.Initialize();
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            Session.GameInfoRequested -= HandleGameInfoRequested;
            Session.JoinRequested -= HandleJoinRequested;

            base.Dispose(disposing);
        }

        #endregion

        #region Events

        protected virtual void HandleGameInfoRequested(object sender, EventArgs e)
        {
        }

        protected virtual void HandleJoinRequested(object sender, EventArgs e)
        {
        }

        #endregion
    }
}
