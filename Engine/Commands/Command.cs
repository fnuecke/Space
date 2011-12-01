using Engine.Serialization;
using Engine.Session;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands.
    /// </summary>
    public abstract class Command<T, TPlayerData, TPacketizerContext> : ICommand<T, TPlayerData, TPacketizerContext>
        where T : struct
        where TPlayerData : IPacketizable<TPacketizerContext>
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
        public T Type { get; private set; }

        #endregion

        #region Constructor

        protected Command(T type)
        {
            Type = type;
        }

        protected Command(T type, Player<TPlayerData, TPacketizerContext> player)
        {
            this.IsAuthoritative = false;
            this.Type = type;
            this.Player = player;
        }

        #endregion

        #region Serialization

        public virtual void Packetize(Packet packet)
        {
        }

        public virtual void Depacketize(Packet packet, TPacketizerContext context)
        {
        }

        #endregion

        #region Equality

        public virtual bool Equals(ICommand<T, TPlayerData, TPacketizerContext> other)
        {
            return other != null && other.Type.Equals(this.Type) &&
                other.Player.Number == this.Player.Number;
        }

        #endregion
    }
}
