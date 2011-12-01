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
    public abstract class AbstractUdpController<TSession, TCommandType, TPlayerData, TPacketizerContext> : DrawableGameComponent
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPacketizerContext>, new()
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
        /// Keyboard input manager.
        /// </summary>
        protected IKeyboardInputManager keyboard;

        /// <summary>
        /// Mouse input manager.
        /// </summary>
        protected IMouseInputManager mouse;

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

        /// <summary>
        /// Initialize the protocol.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        /// <param name="port">the port to listen on.</param>
        /// <param name="header">the protocol header.</param>
        public AbstractUdpController(Game game, ushort port, string header)
            : base(game)
        {
            protocol = new UdpProtocol(port, Encoding.ASCII.GetBytes(header));
        }

        /// <summary>
        /// Attach ourselves as listeners.
        /// </summary>
        public override void Initialize()
        {
            console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));
            keyboard = (IKeyboardInputManager)Game.Services.GetService(typeof(IKeyboardInputManager));
            mouse = (IMouseInputManager)Game.Services.GetService(typeof(IMouseInputManager));
            packetizer = (IPacketizer<TPacketizerContext>)Game.Services.GetService(typeof(IPacketizer<TPacketizerContext>));

            if (keyboard != null)
            {
                keyboard.Pressed += HandleKeyPressed;
                keyboard.Released += HandleKeyReleased;
            }
            if (mouse != null)
            {
                mouse.Pressed += HandleMousePressed;
                mouse.Released += HandleMouseReleased;
                mouse.Scrolled += HandleMouseScrolled;
                mouse.Moved += HandleMouseMoved;
            }

            Session.PlayerData += HandlePlayerData;
            Session.PlayerJoined += HandlePlayerJoined;
            Session.PlayerLeft += HandlePlayerLeft;

            Game.Components.Add(Session);

            base.Initialize();
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            keyboard.Pressed -= HandleKeyPressed;
            keyboard.Released -= HandleKeyReleased;

            Session.PlayerData -= HandlePlayerData;
            Session.PlayerJoined -= HandlePlayerJoined;
            Session.PlayerLeft -= HandlePlayerLeft;

            protocol.Dispose();
            Session.Dispose();

            Game.Components.Remove(Session);

            base.Dispose(disposing);
        }

        #endregion

        #region Logic

        /// <summary>
        /// Drive the network protocol.
        /// </summary>
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
            Session.Send(WrapDataForSend(command, new Packet()), pollRate);
        }

        /// <summary>
        /// Send a command to another client.
        /// </summary>
        /// <param name="player">the player to send the command to.</param>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        public void Send(int player, ICommand<TCommandType, TPlayerData, TPacketizerContext> command, uint pollRate = 0)
        {
            Session.Send(player, WrapDataForSend(command, new Packet()), pollRate);
        }

        /// <summary>
        /// Send a command to everyone, including the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        public void SendAll(ICommand<TCommandType, TPlayerData, TPacketizerContext> command, uint pollRate = 0)
        {
            Session.SendAll(WrapDataForSend(command, new Packet()), pollRate);
        }

        #endregion

        #region Events to be handled in subclasses

        /// <summary>
        /// The local player pressed a keyboard key.
        /// </summary>
        protected virtual void HandleKeyPressed(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// The local player released a keyboard key.
        /// </summary>
        protected virtual void HandleKeyReleased(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// The local player pressed a mouse button.
        /// </summary>
        protected virtual void HandleMousePressed(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// The local player released a mouse button.
        /// </summary>
        protected virtual void HandleMouseReleased(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// The local player scrolled the mousewheel.
        /// </summary>
        protected virtual void HandleMouseScrolled(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// The local player moved the mouse.
        /// </summary>
        protected virtual void HandleMouseMoved(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Another player joined the game.
        /// </summary>
        protected virtual void HandlePlayerJoined(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Another player left the game.
        /// </summary>
        protected virtual void HandlePlayerLeft(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Implement in subclasses to handle commands sent by other clients or
        /// the server.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        /// <returns>whether the command was handled successfully (<c>true</c>) or not (<c>false</c>).</returns>
        protected virtual bool HandleCommand(ICommand<TCommandType, TPlayerData, TPacketizerContext> command)
        {
            return false;
        }

        #endregion

        #region Events handled internally

        /// <summary>
        /// We received some data from another client in the session. This method
        /// assumes all messages in the session are sent via our <c>Send()</c>
        /// methods, i.e. that only commands are sent. We try to parse these here,
        /// then forward them to the <c>HandleCommand()</c> method.
        /// 
        /// <para>
        /// To take influence on how messages are sent and received (add another
        /// layer to the protocol), use the <c>WrapDataForSend()</c> and
        /// <c>UnwrapDataForReceive()</c> methods.
        /// </para>
        /// </summary>
        protected void HandlePlayerData(object sender, EventArgs e)
        {
            try
            {
                var args = (PlayerDataEventArgs<TPlayerData, TPacketizerContext>)e;

                ICommand<TCommandType, TPlayerData, TPacketizerContext> command;

                // Delegate unwrapping of the message, and if this yields a command object
                // try to handle it.
                if (UnwrapDataForReceive(args, out command) && (command == null || HandleCommand(command)))
                {
                    // If this was successfully handled, mark it as consumed.
                    args.Consume();
                }
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

        #endregion

        #region Send / Receive exentsibility

        /// <summary>
        /// May be overridden in subclasses which wish to add another protocol layer.
        /// In that case this should follow the pattern
        /// <code>
        /// override PrepareForSend(...) {
        ///   packet.Write(myStuff);
        ///   return base.PrepareForSend(...);
        /// }
        /// </code>
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        protected virtual Packet WrapDataForSend(ICommand<TCommandType, TPlayerData, TPacketizerContext> command, Packet packet)
        {
            packet.Write((command.Player == null) ? Session.LocalPlayerNumber : command.Player.Number);
            packetizer.Packetize(command, packet);
            return packet;
        }

        /// <summary>
        /// May be overridden to implement the other end of a protocol layer as
        /// added via <c>WrapDataForSend()</c>. You should follow the same pattern
        /// as there.
        /// </summary>
        /// <param name="args">the originally received network data.</param>
        /// <param name="command">the parsed command, or null, if the message
        /// was not a command (i.e. some other message type).</param>
        /// <returns>if the message was handled successfully.</returns>
        protected virtual bool UnwrapDataForReceive(PlayerDataEventArgs<TPlayerData, TPacketizerContext> args, out ICommand<TCommandType, TPlayerData, TPacketizerContext> command)
        {
            // Get the player that issued the command.
            int playerNumber = args.Data.ReadInt32();
            if (!args.IsFromServer)
            {
                // Avoid clients injecting commands for other clients.
                playerNumber = args.Player.Number;
            }

            // Parse the actual command.
            command = packetizer.Depacketize<ICommand<TCommandType, TPlayerData, TPacketizerContext>>(args.Data);

            // Flag it accordingly to where it came from.
            command.IsAuthoritative = args.IsFromServer;

            // Set the issuing player.
            command.Player = Session.GetPlayer(playerNumber);

            return true;
        }

        #endregion
    }
}
