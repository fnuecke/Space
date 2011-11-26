using System;
using System.Text;
using Engine.Network;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;
using Space.Model;
using SpaceData;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the server side.
    /// </summary>
    class Server : GameComponent
    {
        /// <summary>
        /// The console to log messages to.
        /// </summary>
        private IGameConsole console;

        /// <summary>
        /// The underlying server session being used.
        /// </summary>
        public IServerSession Session { get; private set; }

        /// <summary>
        /// The network protocol we'll use.
        /// </summary>
        private UdpProtocol protocol;

        /// <summary>
        /// The static base information about the game world.
        /// </summary>
        StaticWorld world;

        /// <summary>
        /// The game state representing the current game world.
        /// </summary>
        private TSS<GameState, IGameObject> simulation;

        public Server(Game game, int maxPlayers, byte worldSize, long worldSeed)
            : base(game)
        {
            protocol = new UdpProtocol(8442, Encoding.ASCII.GetBytes("5p4c3"));
            Session = SessionFactory.StartServer(protocol, maxPlayers);

            console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));

            world = new StaticWorld(worldSize, worldSeed, Game.Content.Load<WorldConstaints>("Data/world"));
            simulation = new TSS<GameState, IGameObject>(new int[] { 50 });
            simulation.Synchronize(new GameState());

            Session.GameInfoRequested += new EventHandler(HandleGameInfoRequested);
            Session.JoinRequested += new EventHandler(HandleJoinRequested);
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

        private void HandleGameInfoRequested(object sender, EventArgs e)
        {
            console.WriteLine("NET: Game info request.");
            var args = (RequestEventArgs)e;
            args.Data.Write("Hello there!");
        }

        private void HandleJoinRequested(object sender, EventArgs e)
        {
            console.WriteLine("NET: Join request.");
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
