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
    public abstract class AbstractController<TSession, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        : DrawableGameComponent, IController<TSession, TCommand, TCommandType, TPlayerData, TPacketizerContext>
        where TSession : ISession<TPlayerData, TPacketizerContext>
        where TCommand : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// The underlying session being used by this controller.
        /// </summary>
        public TSession Session { get; private set; }

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
        public AbstractController(Game game, TSession session)
            : base(game)
        {
            this.Session = session;
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

                Game.Components.Remove(Session);
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Events to be handled in subclasses

        /// <summary>
        /// Implement in subclasses to handle commands sent by other clients or
        /// the server.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        /// <returns>whether the command was handled successfully (<c>true</c>) or not (<c>false</c>).</returns>
        protected abstract bool HandleRemoteCommand(TCommand command);

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
            // Parse the actual command.
            command = Packetizer.Depacketize<TCommand>(args.Data);

            // If the player is not the server, and the number doesn't match,
            // ignore the command -> avoid clients injecting commands for
            // other clients.
            if (!args.IsFromServer && !args.Player.Equals(command.Player))
            {
                return false;
            }

            // Flag it accordingly to where it came from.
            command.IsAuthoritative = args.IsFromServer;

            return true;
        }

        #endregion

        #region Commands

        /// <summary>
        /// Send a command to the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        protected void SendToHost(TCommand command, PacketPriority priority)
        {
            Session.SendToHost(WrapDataForSend(command, new Packet()), priority);
        }

        /// <summary>
        /// Send a command to another client.
        /// </summary>
        /// <param name="player">the player to send the command to.</param>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        protected void SendToPlayer(Player<TPlayerData, TPacketizerContext> player, TCommand command, PacketPriority priority)
        {
            Session.SendToPlayer(player, WrapDataForSend(command, new Packet()), priority);
        }

        /// <summary>
        /// Send a command to everyone, including the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        /// <param name="pollRate">resend interval until ack arrived.</param>
        protected void SendToEveryone(TCommand command, PacketPriority priority)
        {
            Session.SendToEveryone(WrapDataForSend(command, new Packet()), priority);
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
