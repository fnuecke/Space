using System;
using Engine.Serialization;
using Engine.Session;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for clients using the UDP network protocol.
    /// </summary>
    public abstract class AbstractUdpClient<TCommandType, TPlayerData, TPacketizerContext> : AbstractUdpController<IClientSession<TPlayerData, TPacketizerContext>, TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>, new()
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Construction / Destruction

        /// <summary>
        /// Initializes the underlying session after initializing
        /// the protocol in the base class.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractUdpClient(Game game, ushort port, string header)
            : base(game, port, header)
        {
            Session = SessionFactory.StartClient<TPlayerData, TPacketizerContext>(game, protocol);
        }

        /// <summary>
        /// Attach ourselves as listeners.
        /// </summary>
        public override void Initialize()
        {
            Session.GameInfoReceived += HandleGameInfoReceived;
            Session.JoinResponse += HandleJoinResponse;

            base.Initialize();
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            Session.GameInfoReceived -= HandleGameInfoReceived;
            Session.JoinResponse -= HandleJoinResponse;

            base.Dispose(disposing);
        }

        #endregion

        #region Events

        protected virtual void HandleGameInfoReceived(object sender, EventArgs e)
        {
        }

        protected virtual void HandleJoinResponse(object sender, EventArgs e)
        {
        }

        #endregion
    }
}
