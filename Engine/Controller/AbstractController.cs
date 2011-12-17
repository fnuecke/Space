using System;
using Engine.Commands;
using Engine.Serialization;
using Engine.Session;
using Engine.Util;
using Microsoft.Xna.Framework;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for all game controller.
    /// </summary>
    public abstract class AbstractController<TSession, TCommand>
        : DrawableGameComponent, IController<TSession>
        where TSession : ISession
        where TCommand : ICommand
    {
        #region Logger

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Properties

        /// <summary>
        /// The underlying session being used by this controller.
        /// </summary>
        public TSession Session { get; private set; }

        /// <summary>
        /// The console to log messages to, which will be the same for all controllers.
        /// </summary>
        protected IGameConsole Console { get; private set; }

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

            if (Session != null)
            {
                Session.Data += HandlePlayerData;
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
                Session.Data -= HandlePlayerData;
                Game.Components.Remove(Session);
                Session.Dispose();
                Session = default(TSession);
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
        /// override WrapDataForSend(...) {
        ///   packet.Write(myStuff);
        ///   return base.WrapDataForSend(...);
        /// }
        /// </code>
        /// </summary>
        /// <param name="command">the command to wrap.</param>
        /// <returns>the given packet, after writing.</returns>
        protected virtual Packet WrapDataForSend(TCommand command, Packet packet)
        {
            packet.Write(command.Player.Number);
            Packetizer.Packetize(command, packet);
            return packet;
        }

        /// <summary>
        /// May be overridden to implement the other end of a protocol layer as
        /// added via <c>WrapDataForSend()</c>. You should follow the same pattern
        /// as there.
        /// </summary>
        /// <param name="args">the originally received network data.</param>
        /// <returns>the parsed command, or null, if the message
        /// was not a command (i.e. some other message type).</returns>
        protected virtual TCommand UnwrapDataForReceive(SessionDataEventArgs args)
        {
            // Parse the actual command.
            Player player = Session.GetPlayer(args.Data.ReadInt32());
            TCommand command = Packetizer.Depacketize<TCommand>(args.Data);
            command.Player = player;
            return command;
        }

        #endregion

        #region Commands

        /// <summary>
        /// Send a command to the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        protected void Send(TCommand command)
        {
            Session.Send(WrapDataForSend(command, new Packet()));
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
                // Delegate unwrapping of the message, and if this yields a command object
                // try to handle it.
                TCommand command = UnwrapDataForReceive((SessionDataEventArgs)e);
                if (command != null)
                {
                    HandleRemoteCommand(command);
                }
            }
            catch (PacketException ex)
            {
                logger.WarnException("Failed parsing received packet.", ex);
            }
            catch (ArgumentException ex)
            {
                logger.WarnException("Failed deserializing unknown type.", ex);
            }
        }

        #endregion
    }
}
