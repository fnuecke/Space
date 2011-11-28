using System;
using Engine.Serialization;
using Engine.Session;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for clients using the UDP network protocol.
    /// </summary>
    public abstract class AbstractUdpClient<TPlayerData, TCommandType> : AbstractUdpController<IClientSession<TPlayerData>, TPlayerData, TCommandType>
        where TPlayerData : IPacketizable, new()
        where TCommandType : struct
    {
        #region Construction / Destruction

        public AbstractUdpClient(Game game, ushort port, string header)
            : base(game, port, header)
        {
            Session = SessionFactory.StartClient<TPlayerData>(game, protocol);
        }

        public override void Initialize()
        {
            Session.GameInfoReceived += HandleGameInfoReceived;
            Session.JoinResponse += HandleJoinResponse;

            base.Initialize();
        }

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
