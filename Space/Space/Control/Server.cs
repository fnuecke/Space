using System;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Space.Model;
using SpaceData;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the server side.
    /// </summary>
    class Server : AbstractUdpServer
    {
        #region Fields

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
            : base(game, maxPlayers)
        {
            world = new StaticWorld(worldSize, worldSeed, Game.Content.Load<WorldConstaints>("Data/world"));
            simulation = new TSS<GameState, IGameObject>(new int[] { 50 });
            simulation.Synchronize(new GameState());
        }

        public override void Update(GameTime gameTime)
        {
            // Drive game logic.
            simulation.Update();

            base.Update(gameTime);
        }

        protected override void HandleGameInfoRequested(object sender, EventArgs e)
        {
            console.WriteLine("SRV.NET: Game info request.");
            var args = (RequestEventArgs)e;
            args.Data.Write("Hello there!");
        }

        protected override void HandleJoinRequested(object sender, EventArgs e)
        {
            console.WriteLine("SRV.NET: Join request.");
        }

        protected override void HandlePlayerData(object sender, EventArgs e)
        {
            var args = (PlayerDataEventArgs)e;
            console.WriteLine(String.Format("SRV.NET: Got data from {0}: {1}", args.Player, args.Data.ReadString()));
            args.Consume();
        }

        protected override void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("SRV.NET: {0} joined.", args.Player));
        }

        protected override void HandlePlayerLeft(object sender, EventArgs e)
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

#endregion
    }
}
