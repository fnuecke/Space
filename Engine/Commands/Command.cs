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
        /// Whether the command is signed (e.g. by a server) (<c>false</c>)
        /// or came from an untrustworthy source (e.g. another client) (<c>true</c>).
        /// 
        /// IMPORTANT: must be set externally, when receiving a command.
        /// </summary>
        public bool IsTentative { get; set; }

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
            this.IsTentative = true;
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
