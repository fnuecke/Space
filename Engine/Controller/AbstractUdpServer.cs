using System;
using System.Text;
using Engine.Network;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for game servers using the UDP network protocol.
    /// </summary>
    public abstract class AbstractUdpServer : GameComponent
    {
        #region Properties

        /// <summary>
        /// The underlying server session being used.
        /// </summary>
        public IServerSession Session { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// The console to log messages to.
        /// </summary>
        protected IGameConsole console;

        /// <summary>
        /// The network protocol we'll use.
        /// </summary>
        protected UdpProtocol protocol;

        #endregion

        #region Construction / Destruction

        public AbstractUdpServer(Game game, int maxPlayers)
            : base(game)
        {
            protocol = new UdpProtocol(8442, Encoding.ASCII.GetBytes("5p4c3"));
            Session = SessionFactory.StartServer(game, protocol, maxPlayers);

            console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));

            game.Components.Add(this);
        }

        public override void Initialize()
        {
            Session.GameInfoRequested += HandleGameInfoRequested;
            Session.JoinRequested += HandleJoinRequested;
            Session.PlayerData += HandlePlayerData;
            Session.PlayerJoined += HandlePlayerJoined;
            Session.PlayerLeft += HandlePlayerLeft;

            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            Session.GameInfoRequested -= HandleGameInfoRequested;
            Session.JoinRequested -= HandleJoinRequested;
            Session.PlayerData -= HandlePlayerData;
            Session.PlayerJoined -= HandlePlayerJoined;
            Session.PlayerLeft -= HandlePlayerLeft;

            protocol.Dispose();
            Session.Dispose();

            Game.Components.Remove(this);

            base.Dispose(disposing);
        }

        #endregion

        /// <summary>
        /// Drives the network protocol.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Drive network communication.
            protocol.Receive();
            protocol.Flush();

            base.Update(gameTime);
        }

        #region Events

        protected abstract void HandleGameInfoRequested(object sender, EventArgs e);

        protected abstract void HandleJoinRequested(object sender, EventArgs e);

        protected abstract void HandlePlayerData(object sender, EventArgs e);

        protected abstract void HandlePlayerJoined(object sender, EventArgs e);

        protected abstract void HandlePlayerLeft(object sender, EventArgs e);

        #endregion
    }
}
