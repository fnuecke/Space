using System;
using System.Text;
using Engine.Network;
using Engine.Session;
using Engine.Simulation;
using Engine.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Model;
using SpaceData;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the server side.
    /// </summary>
    class Server : GameComponent
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
        private IGameConsole console;

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

        #endregion

        public Server(Game game, int maxPlayers, byte worldSize, long worldSeed)
            : base(game)
        {
            protocol = new UdpProtocol(8442, Encoding.ASCII.GetBytes("5p4c3"));
            Session = SessionFactory.StartServer(protocol, maxPlayers);

            console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));

            world = new StaticWorld(worldSize, worldSeed, Game.Content.Load<WorldConstaints>("Data/world"));
            simulation = new TSS<GameState, IGameObject>(new int[] { 50 });
            simulation.Synchronize(new GameState());

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

        public override void Update(GameTime gameTime)
        {
            // Drive network communication.
            protocol.Receive();
            protocol.Flush();

            // Drive game logic.
            simulation.Update();

            base.Update(gameTime);
        }

        private void HandleGameInfoRequested(object sender, EventArgs e)
        {
            console.WriteLine("SRV.NET: Game info request.");
            var args = (RequestEventArgs)e;
            args.Data.Write("Hello there!");
        }

        private void HandleJoinRequested(object sender, EventArgs e)
        {
            console.WriteLine("SRV.NET: Join request.");
        }

        private void HandlePlayerData(object sender, EventArgs e)
        {
            var args = (PlayerDataEventArgs)e;
            console.WriteLine(String.Format("SRV.NET: Got data from {0}: {1}", args.Player, args.Data.ReadString()));
            args.Consume();
        }

        private void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("SRV.NET: {0} joined.", args.Player));
        }

        private void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("SRV.NET: {0} left.", args.Player));
        }

#region Debugging stuff

        internal void DEBUG_DrawInfo(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            SpriteFont font = Game.Content.Load<SpriteFont>("Fonts/ConsoleFont");
            string text = "Server (" + Session.NumPlayers + "/" + Session.MaxPlayers + ")";
            for (int i = 0; i < Session.NumPlayers; ++i)
            {
                Player player = Session.GetPlayer(i);
                text += "\n#" + player.Number + ": " + player.Name + " [" + player.Ping + "]";
            }

            spriteBatch.Begin();

            spriteBatch.DrawString(font, text, new Vector2(10, Game.GraphicsDevice.Viewport.Height - font.MeasureString(text).Y - 10), Color.White);

            spriteBatch.End();
        }
    }

#endregion
}
