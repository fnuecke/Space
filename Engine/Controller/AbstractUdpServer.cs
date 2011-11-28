using System;
using Engine.Serialization;
using Engine.Session;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for game servers using the UDP network protocol.
    /// </summary>
    public abstract class AbstractUdpServer<TPlayerData, TCommandType> : AbstractUdpController<IServerSession<TPlayerData>, TPlayerData, TCommandType>
        where TPlayerData : IPacketizable, new()
        where TCommandType : struct
    {
        #region Construction / Destruction

        public AbstractUdpServer(Game game, int maxPlayers, ushort port, string header)
            : base(game, port, header)
        {
            Session = SessionFactory.StartServer<TPlayerData>(game, protocol, maxPlayers);
        }

        public override void Initialize()
        {
            Session.GameInfoRequested += HandleGameInfoRequested;
            Session.JoinRequested += HandleJoinRequested;

            base.Initialize();
        }

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
