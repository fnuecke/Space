using System;
using Engine.Serialization;
using Engine.Session;
using Engine.Simulation.Commands;

namespace Engine.Controller
{
    /// <summary>
    /// Base class for all game controller.
    /// </summary>
    public abstract class AbstractController<TSession, TCommand> : IController<TSession>
        where TSession : ISession
        where TCommand : Command
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion

        #region Properties

        /// <summary>
        /// The underlying session being used by this controller.
        /// </summary>
        public TSession Session { get; private set; }

        /// <summary>
        /// The current 'load', i.e. how much of the available time is actually
        /// needed to perform an update.
        /// </summary>
        public abstract float CurrentLoad { get; }

        #endregion

        #region Construction / Destruction

        /// <summary>
        /// Initialize the controller.
        /// </summary>
        /// <param name="session">The session to use.</param>
        protected AbstractController(TSession session)
        {
            Session = session;
            Session.Data += HandlePlayerData;
        }

        /// <summary>
        /// Dispose this controller.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Remove ourselves as listeners.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            Session.Data -= HandlePlayerData;
        }

        #endregion

        #region Logic

        /// <summary>
        /// Called when the controller needs to be updated.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds since the last call.</param>
        public virtual void Update(float elapsedMilliseconds)
        {
        }

        /// <summary>
        /// Called when the controller needs to be rendered.
        /// </summary>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds since the last call.</param>
        public virtual void Draw(float elapsedMilliseconds)
        {
        }

        #endregion

        #region Events to be handled in subclasses

        /// <summary>
        /// Implement in subclasses to handle commands sent by other clients or
        /// the server.
        /// </summary>
        /// <param name="command">the command to handle.</param>
        /// <returns>whether the command was handled successfully (<c>true</c>) or not (<c>false</c>).</returns>
        protected abstract void HandleRemoteCommand(TCommand command);

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
        /// <param name="command">The command to wrap.</param>
        /// <param name="packet">The packet to wrap into.</param>
        /// <returns>the given packet, after writing.</returns>
        protected virtual Packet WrapDataForSend(TCommand command, Packet packet)
        {
            return packet.WriteWithTypeInfo(command);
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
            return args.Data.ReadPacketizableWithTypeInfo<TCommand>();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Send a command to the server.
        /// </summary>
        /// <param name="command">the command to send.</param>
        protected void Send(TCommand command)
        {
            using (var packet = new Packet())
            {
                Session.Send(WrapDataForSend(command, packet));
            }
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
        private void HandlePlayerData(object sender, SessionDataEventArgs e)
        {
            try
            {
                // Delegate unwrapping of the message, and if this yields a command object
                // try to handle it.
                var command = UnwrapDataForReceive(e);
                if (command != null)
                {
                    HandleRemoteCommand(command);
                }
            }
            catch (PacketException ex)
            {
                Logger.WarnException("Failed parsing received packet.", ex);
            }
            catch (Exception ex)
            {
                Logger.WarnException("Failed deserializing data.", ex);
            }
        }

        #endregion
    }
}
