using System;
using System.Text;
using Engine.Network;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.Model;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the client side.
    /// </summary>
    class Client : GameComponent
    {
        /// <summary>
        /// The console to log messages to.
        /// </summary>
        private IGameConsole console;

        /// <summary>
        /// The underlying client session being used.
        /// </summary>
        public IClientSession Session { get; private set; }

        /// <summary>
        /// The network protocol we'll use.
        /// </summary>
        private UdpProtocol protocol;

        /// <summary>
        /// The game state representing the current game world.
        /// </summary>
        private TSS<GameState, IGameObject> simulation;

        public Client(Game game)
            : base(game)
        {
            protocol = new UdpProtocol(8443, Encoding.ASCII.GetBytes("5p4c3"));
            Session = SessionFactory.StartClient(protocol);

            console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));

            simulation = new TSS<GameState, IGameObject>(new int[] { 50 });

            Session.GameInfoReceived += new EventHandler(HandleGameInfoReceived);
            Session.JoinResponse += new EventHandler(HandleJoinResponse);
            Session.PlayerData += new EventHandler(HandlePlayerData);
            Session.PlayerJoined += new EventHandler(HandlePlayerJoined);
            Session.PlayerLeft += new EventHandler(HandlePlayerLeft);

            game.Components.Add(this);
        }

        protected override void Dispose(bool disposing)
        {
            protocol.Dispose();
            Session.Dispose();
            base.Dispose(disposing);
        }

        public override void Update(GameTime gameTime)
        {
            // Drive network communication.
            protocol.Receive();
            protocol.Flush();

            // Drive game logic.


            base.Update(gameTime);
        }

        private void HandleGameInfoReceived(object sender, EventArgs e)
        {
            var args = (GameInfoReceivedEventArgs)e;
            var info = args.Data.ReadString();
            console.WriteLine(String.Format("Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers));
        }

        private void HandleJoinResponse(object sender, EventArgs e)
        {
            var args = (JoinResponseEventArgs)e;
            console.WriteLine(string.Format("Join response: {0} ({1})", args.WasSuccess, Enum.GetName(typeof(JoinResponseReason), args.Reason)));
        }

        private void HandlePlayerData(object sender, EventArgs e)
        {
            var args = (PlayerDataEventArgs)e;
            console.WriteLine(String.Format("Got data from {0}: {1}", args.Player, args.Data.ReadString()));
            args.Consume();
        }

        private void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("{0} joined.", args.Player));
        }

        private void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("{0} left.", args.Player));
        }
    }
}
