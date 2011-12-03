using Engine.Serialization;
using Engine.Session;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands.
    /// </summary>
    public abstract class Command<TCommandType, TPlayerData, TPacketizerContext> : ICommand<TCommandType, TPlayerData, TPacketizerContext>
        where TCommandType : struct
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        #region Properties

        /// <summary>
        /// Whether the command is signed (e.g. by a server) (<c>true</c>)
        /// or came from an untrustworthy source (e.g. another client) (<c>false</c>).
        /// </summary>
        public bool IsAuthoritative { get; set; }

        /// <summary>
        /// The player that issued the command.
        /// </summary>
        public Player<TPlayerData, TPacketizerContext> Player { get; set; }

        /// <summary>
        /// The type of the command.
        /// </summary>
        public TCommandType Type { get; private set; }

        #endregion

        #region Constructor

        protected Command(TCommandType type)
        {
            this.Type = type;
        }

        #endregion

        #region Serialization

        public virtual void Packetize(Packet packet)
        {
            packet.Write(Player != null ? Player.Number : -1);
        }

        public virtual void Depacketize(Packet packet, TPacketizerContext context)
        {
            Player = context.Session.GetPlayer(packet.ReadInt32());
        }

        #endregion

        #region Equality

        public virtual bool Equals(ICommand<TCommandType, TPlayerData, TPacketizerContext> other)
        {
            return other != null && other.Type.Equals(this.Type) &&
                other.Player.Number == this.Player.Number;
        }

        #endregion
    }
}
