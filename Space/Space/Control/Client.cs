using System;
using Engine.Controller;
using Engine.Session;
using Engine.Simulation;
using Microsoft.Xna.Framework;
using Space.Model;

namespace Space.Control
{
    /// <summary>
    /// Handles game logic on the client side.
    /// </summary>
    class Client : AbstractUdpClient
    {
        #region Fields

        /// <summary>
        /// The game state representing the current game world.
        /// </summary>
        private TSS<GameState, IGameObject> simulation;

        #endregion

        public Client(Game game)
            : base(game)
        {
            simulation = new TSS<GameState, IGameObject>(new int[] { 50 });
        }

        public override void Update(GameTime gameTime)
        {
            // Drive game logic.


            base.Update(gameTime);
        }

        protected override void HandleKeyReleased(object sender, EventArgs e)
        {
        }

        protected override void HandleKeyPressed(object sender, EventArgs e)
        {
        }

        protected override void HandleGameInfoReceived(object sender, EventArgs e)
        {
            var args = (GameInfoReceivedEventArgs)e;
            var info = args.Data.ReadString();
            console.WriteLine(String.Format("CLT.NET: Found a game: [{0}] {1} ({2}/{3})", args.Host.ToString(), info, args.NumPlayers, args.MaxPlayers));
        }

        protected override void HandleJoinResponse(object sender, EventArgs e)
        {
            var args = (JoinResponseEventArgs)e;
            console.WriteLine(string.Format("CLT.NET: Join response: {0} ({1})", args.WasSuccess, Enum.GetName(typeof(JoinResponseReason), args.Reason)));
        }

        protected override void HandlePlayerData(object sender, EventArgs e)
        {
            var args = (PlayerDataEventArgs)e;
            console.WriteLine(String.Format("CLT.NET: Got data from {0}: {1}", args.Player, args.Data.ReadString()));
            args.Consume();
        }

        protected override void HandlePlayerJoined(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("CLT.NET: {0} joined.", args.Player));
        }

        protected override void HandlePlayerLeft(object sender, EventArgs e)
        {
            var args = (PlayerEventArgs)e;
            console.WriteLine(String.Format("CLT.NET: {0} left.", args.Player));
        }
    }
}
