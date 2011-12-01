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
    public abstract class AbstractUdpController<TSession, TPlayerData, TCommandType, TPacketizerContext> : GameComponent
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TPlayerData : IPacketizable<TPacketizerContext>, new()
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
        /// Packetizer used for this session's game.
        /// </summary>
        protected IPacketizer<TPacketizerContext> packetizer;

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
            packetizer = (IPacketizer<TPacketizerContext>)Game.Services.GetService(typeof(IPacketizer<TPacketizerContext>));

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

        #region Send methods

        /// <summary>
        /// Send a command to the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        public void Send(ICommand<TCommandType, TPlayerData, TPacketizerContext> command, uint pollRate = 0)
        {
            Packet packet = new Packet();
            packet.Write((command.Player == null) ? Session.LocalPlayerNumber : command.Player.Number);
            packetizer.Packetize(command, packet);
            Session.Send(packet, pollRate);
        }

        /// <summary>
        /// Send a command to another client.
        /// </summary>
        /// <param name="player">the player to send the command to.</param>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        public void Send(int player, ICommand<TCommandType, TPlayerData, TPacketizerContext> command, uint pollRate = 0)
        {
            Packet packet = new Packet();
            packet.Write((command.Player == null) ? Session.LocalPlayerNumber : command.Player.Number);
            packetizer.Packetize(command, packet);
            Session.Send(player, packet, pollRate);
        }

        /// <summary>
        /// Send a command to everyone, including the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        public void SendAll(ICommand<TCommandType, TPlayerData, TPacketizerContext> command, uint pollRate = 0)
        {
            Packet packet = new Packet();
            packet.Write((command.Player == null) ? Session.LocalPlayerNumber : command.Player.Number);
            packetizer.Packetize(command, packet);
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
                var args = (PlayerDataEventArgs<TPlayerData, TPacketizerContext>)e;

                // Get the player that issued the command.
                int playerNumber = args.Data.ReadInt32();
                if (!args.IsFromServer)
                {
                    // Avoid clients injecting commands for other clients.
                    playerNumber = args.Player.Number;
                }
                
                // Parse the actual command.
                ICommand<TCommandType, TPlayerData, TPacketizerContext> command = packetizer.Depacketize<ICommand<TCommandType, TPlayerData, TPacketizerContext>>(args.Data);

                // Flag it accordingly to where it came from.
                command.IsTentative = !args.IsFromServer;

                // Set the issuing player.
                command.Player = Session.GetPlayer(playerNumber);

                // Handle it.
                HandleCommand(command);

                // If this was successful (no exception), mark it as consumed.
                args.Consume();
            }
#if DEBUG
            catch (PacketException ex)
            {
                Console.WriteLine("Error handling received player data: " + ex);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Error handling received player data: " + ex);
            }
#else
            catch (PacketException)
            {
            }
            catch (ArgumentException)
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

        protected virtual void HandleCommand(ICommand<TCommandType, TPlayerData, TPacketizerContext> command)
        {
        }

        #endregion
    }
}
