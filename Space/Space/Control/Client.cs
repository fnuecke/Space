using System;
using System.Text;
using Engine.Input;
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
        #region Properties

        /// <summary>
        /// The underlying client session being used.
        /// </summary>
        public IClientSession Session { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// The console to log messages to.
        /// </summary>
        private IGameConsole console;

        /// <summary>
        /// Input manager.
        /// </summary>
        private IKeyboardInputManager input;

        /// <summary>
        /// The network protocol we'll use.
        /// </summary>
        private UdpProtocol protocol;

        /// <summary>
        /// The game state representing the current game world.
        /// </summary>
        private TSS<GameState, IGameObject> simulation;

        #endregion

        public Client(Game game)
            : base(game)
        {
            protocol = new UdpProtocol(8443, Encoding.ASCII.GetBytes("5p4c3"));
            Session = SessionFactory.StartClient(protocol);
            simulation = new TSS<GameState, IGameObject>(new int[] { 50 });

            game.Components.Add(this);
        }

        public override void Initialize()
        {
            console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));
            input = (IKeyboardInputManager)Game.Services.GetService(typeof(IKeyboardInputManager));

            input.Pressed += HandleKeyPressed;
            input.Released += HandleKeyReleased;

            Session.GameInfoReceived += HandleGameInfoReceived;
            Session.JoinResponse += HandleJoinResponse;
            Session.PlayerData += HandlePlayerData;
            Session.PlayerJoined += HandlePlayerJoined;
            Session.PlayerLeft += HandlePlayerLeft;

            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            input.Pressed -= HandleKeyPressed;
            input.Released -= HandleKeyReleased;

            Session.GameInfoReceived -= HandleGameInfoReceived;
            Session.JoinResponse -= HandleJoinResponse;
            Session.PlayerData -= HandlePlayerData;
            Session.PlayerJoined -= HandlePlayerJoined;
            Session.PlayerLeft -= HandlePlayerLeft;

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

        void HandleKeyReleased(object sender, EventArgs e)
        {
        }

        void HandleKeyPressed(object sender, EventArgs e)
        {
        }

        private void HandleGameInfoReceived(object sender, EventArgs e)
        {
            var args = (GameInfoReceivedEventArgs)e;
            var info = args.Data.ReadString();
            console.WriteLine(String.Format("CLT.NET: Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers));
        }

        private void HandleJoinResponse(object sender, EventArgs e)
        {
            var args = (JoinResponseEventArgs)e;
            console.WriteLine(string.Format("CLT.NET: Join response: {0} ({1})", args.WasSuccess, Enum.GetName(typeof(JoinResponseReason), args.Reason)));
        }

        private void HandlePlayerData(object sender, EventArgs e)
        {
            var args = (PlayerDataEventArgs)e;
            console.WriteLine(String.Format("CLT.NET: Got data from {0}: {1}", args.Player, args.Data.ReadString()));
            args.Consume();
        }

        private void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("CLT.NET: {0} joined.", args.Player));
        }

        private void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("CLT.NET: {0} left.", args.Player));
        }
    }
}
