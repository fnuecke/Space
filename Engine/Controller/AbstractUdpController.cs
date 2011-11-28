using System;
using System.Text;
using Engine.Commands;
using Engine.Input;
using Engine.Network;
using Engine.Serialization;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for UDP driven clients and servers.
    /// </summary>
    public class AbstractUdpController<TSession, TPlayerData, TCommandType> : GameComponent
        where TSession : ISession<TPlayerData>
        where TPlayerData : IPacketizable, new()
        where TCommandType : struct
    {
        #region Properties

        /// <summary>
        /// The underlying client session being used.
        /// </summary>
        public TSession Session { get; protected set; }

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

        public AbstractUdpController(Game game, ushort port, string header)
            : base(game)
        {
            protocol = new UdpProtocol(port, Encoding.ASCII.GetBytes(header));
        }

        public override void Initialize()
        {
            console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));
            input = (IKeyboardInputManager)Game.Services.GetService(typeof(IKeyboardInputManager));

            input.Pressed += HandleKeyPressed;
            input.Released += HandleKeyReleased;

            Session.PlayerData += HandlePlayerData;
            Session.PlayerJoined += HandlePlayerJoined;
            Session.PlayerLeft += HandlePlayerLeft;

            Game.Components.Add(Session);

            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            input.Pressed -= HandleKeyPressed;
            input.Released -= HandleKeyReleased;

            Session.PlayerData -= HandlePlayerData;
            Session.PlayerJoined -= HandlePlayerJoined;
            Session.PlayerLeft -= HandlePlayerLeft;

            protocol.Dispose();
            Session.Dispose();

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        public override void Update(GameTime gameTime)
        {
            // Drive network communication.
            protocol.Receive();
            protocol.Flush();

            base.Update(gameTime);
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Send a command to the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        public void Send(ICommand<TCommandType> command, uint pollRate = 0)
        {
            Packet packet = new Packet();
            Packetizer.Packetize(command, packet);
            Session.Send(packet, pollRate);
        }

        /// <summary>
        /// Send a command to another client.
        /// </summary>
        /// <param name="player">the player to send the command to.</param>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        public void Send(int player, ICommand<TCommandType> command, uint pollRate = 0)
        {
            Packet packet = new Packet();
            Packetizer.Packetize(command, packet);
            Session.Send(player, packet, pollRate);
        }

        /// <summary>
        /// Send a command to everyone, including the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        public void SendAll(ICommand<TCommandType> command, uint pollRate = 0)
        {
            Packet packet = new Packet();
            Packetizer.Packetize(command, packet);
            Session.SendAll(packet, pollRate);
        }

        #endregion

        #region Events

        protected virtual void HandleKeyReleased(object sender, EventArgs e)
        {
        }

        protected virtual void HandleKeyPressed(object sender, EventArgs e)
        {
        }

        protected virtual void HandlePlayerData(object sender, EventArgs e)
        {
            try
            {
                var args = (PlayerDataEventArgs<TPlayerData>)e;
                ICommand<TCommandType> command = Packetizer.Depacketize<ICommand<TCommandType>>(args.Data);
                HandleCommand(command);
                args.Consume();
            }
#if DEBUG
            catch (PacketException ex)
            {
                Console.WriteLine("Error handling received player data: " + ex);
            }
#else
            catch (PacketException)
            {
            }
#endif
        }

        protected virtual void HandlePlayerJoined(object sender, EventArgs e)
        {
        }

        protected virtual void HandlePlayerLeft(object sender, EventArgs e)
        {
        }

        protected virtual void HandleCommand(ICommand<TCommandType> command)
        {
        }

        #endregion
    }
}
