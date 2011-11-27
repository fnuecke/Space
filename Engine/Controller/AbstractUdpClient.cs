using System;
using System.Text;
using Engine.Input;
using Engine.Network;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for clients using the UDP network protocol.
    /// </summary>
    public abstract class AbstractUdpClient : GameComponent
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
        protected IGameConsole console;

        /// <summary>
        /// Input manager.
        /// </summary>
        protected IKeyboardInputManager input;

        /// <summary>
        /// The network protocol we'll use.
        /// </summary>
        protected UdpProtocol protocol;

        #endregion

        #region Construction / Destruction

        public AbstractUdpClient(Game game)
            : base(game)
        {
            protocol = new UdpProtocol(8443, Encoding.ASCII.GetBytes("5p4c3"));
            Session = SessionFactory.StartClient(game, protocol);

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

            Game.Components.Remove(this);

            base.Dispose(disposing);
        }

        #endregion

        public override void Update(GameTime gameTime)
        {
            // Drive network communication.
            protocol.Receive();
            protocol.Flush();

            base.Update(gameTime);
        }

        #region Events

        protected abstract void HandleKeyReleased(object sender, EventArgs e);

        protected abstract void HandleKeyPressed(object sender, EventArgs e);

        protected abstract void HandleGameInfoReceived(object sender, EventArgs e);

        protected abstract void HandleJoinResponse(object sender, EventArgs e);

        protected abstract void HandlePlayerData(object sender, EventArgs e);

        protected abstract void HandlePlayerJoined(object sender, EventArgs e);

        protected abstract void HandlePlayerLeft(object sender, EventArgs e);

        #endregion
    }
}
