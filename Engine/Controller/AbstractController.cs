using System;
using Engine.Commands;
using Engine.Network;
using Engine.Serialization;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for all game controller.
    /// </summary>
    public abstract class AbstractController<TSession, TProtocol, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        : DrawableGameComponent, IController<TSession, TProtocol, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TProtocol : IProtocol
        where TCommand : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// The underlying session being used by this controller.
        /// </summary>
        public TSession Session { get; protected set; }

        /// <summary>
        /// The network protocol in use by the session of this controller.
        /// </summary>
        public TProtocol Protocol { get; protected set; }

        /// <summary>
        /// The console to log messages to, which will be the same for all controllers.
        /// </summary>
        protected IGameConsole Console { get; private set; }

        /// <summary>
        /// Packetizer used for the game session handled in this controller.
        /// </summary>
        protected IPacketizer<TPlayerData, TPacketizerContext> Packetizer { get; private set; }

        #endregion

        #region Construction / Destruction

        /// <summary>
        /// Initialize the controller.
        /// </summary>
        /// <param name="game">the game this belongs to.</param>
        public AbstractController(Game game)
            : base(game)
        {
        }

        /// <summary>
        /// Attach ourselves as listeners.
        /// </summary>
        public override void Initialize()
        {
            Console = (IGameConsole)Game.Services.GetService(typeof(IGameConsole));
            Packetizer = ((IPacketizer<TPlayerData, TPacketizerContext>)Game.Services.GetService(typeof(IPacketizer<TPlayerData, TPacketizerContext>))).CopyFor(Session);

            if (Session != null)
            {
                Session.PlayerData += HandlePlayerData;
                Session.PlayerJoined += HandlePlayerJoined;
                Session.PlayerLeft += HandlePlayerLeft;

                Game.Components.Add(Session);
            }

            base.Initialize();
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (Session != null)
            {
                Session.PlayerData -= HandlePlayerData;
                Session.PlayerJoined -= HandlePlayerJoined;
                Session.PlayerLeft -= HandlePlayerLeft;

                Game.Components.Remove(Session);
            }

            if (Protocol != null)
            {
                Protocol.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Events to be handled in subclasses

        /// <summary>
        /// Another player joined the game.
        /// </summary>
        protected abstract void HandlePlayerJoined(object sender, EventArgs e);

        /// <summary>
        /// Another player left the game.
        /// </summary>
        protected abstract void HandlePlayerLeft(object sender, EventArgs e);

        /// <summary>
        /// Implement in subclasses to handle commands generated locally.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        protected abstract void HandleLocalCommand(TCommand command);

        /// <summary>
        /// Implement in subclasses to handle commands sent by other clients or
        /// the server.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        /// <returns>whether the command was handled successfully (<c>true</c>) or not (<c>false</c>).</returns>
        protected abstract bool HandleRemoteCommand(TCommand command);

        /// <summary>
        /// A command emitter we're attached to has generated a new event.
        /// Override this to fill in some default values in the command
        /// before it is passed on to <c>HandleLocalCommand</c>.
        /// </summary>
        protected virtual void HandleEmittedCommand(TCommand command)
        {
            command.Player = Session.LocalPlayer;
            HandleLocalCommand(command);
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
        /// <param name="command">the command to wrap.</param>
        /// <returns>the given packet, after writing.</returns>
        protected virtual Packet WrapDataForSend(TCommand command, Packet packet)
        {
            packet.Write((command.Player == null) ? Session.LocalPlayerNumber : command.Player.Number);
            Packetizer.Packetize(command, packet);
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
        protected virtual bool UnwrapDataForReceive(PlayerDataEventArgs<TPlayerData, TPacketizerContext> args, out TCommand command)
        {
            // Get the player that issued the command.
            int playerNumber = args.Data.ReadInt32();
            if (!args.IsFromServer)
            {
                // Avoid clients injecting commands for other clients.
                playerNumber = args.Player.Number;
            }

            // Parse the actual command.
            command = Packetizer.Depacketize<TCommand>(args.Data);

            // Flag it accordingly to where it came from.
            command.IsAuthoritative = args.IsFromServer;

            // Set the issuing player.
            command.Player = Session.GetPlayer(playerNumber);

            return true;
        }

        #endregion

        #region Commands

        /// <summary>
        /// Add this controller as a listener to the given emitter, handling
        /// whatever commands it produces.
        /// </summary>
        /// <param name="emitter">the emitter to attach to.</param>
        public void AddEmitter(ICommandEmitter<TCommand, TCommandType, TPlayerData, TPacketizerContext> emitter)
        {
            emitter.CommandEmitted += HandleEmittedCommand;
        }

        /// <summary>
        /// Remove this controller as a listener from the given emitter.
        /// </summary>
        /// <param name="emitter">the emitter to detach from.</param>
        public void RemoveEmitter(ICommandEmitter<TCommand, TCommandType, TPlayerData, TPacketizerContext> emitter)
        {
            emitter.CommandEmitted -= HandleEmittedCommand;
        }

        /// <summary>
        /// Send a command to the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        protected void Send(TCommand command, uint pollRate = 0)
        {
            Session.Send(WrapDataForSend(command, new Packet()), pollRate);
        }

        /// <summary>
        /// Send a command to another client.
        /// </summary>
        /// <param name="player">the player to send the command to.</param>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        protected void Send(int player, TCommand command, uint pollRate = 0)
        {
            Session.Send(player, WrapDataForSend(command, new Packet()), pollRate);
        }

        /// <summary>
        /// Send a command to everyone, including the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        protected void SendAll(TCommand command, uint pollRate = 0)
        {
            Session.SendAll(WrapDataForSend(command, new Packet()), pollRate);
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
        private void HandlePlayerData(object sender, EventArgs e)
        {
            try
            {
                var args = (PlayerDataEventArgs<TPlayerData, TPacketizerContext>)e;

                TCommand command;

                // Delegate unwrapping of the message, and if this yields a command object
                // try to handle it.
                if (UnwrapDataForReceive(args, out command) &&
                    (command == null || HandleRemoteCommand(command)))
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
    }
}
