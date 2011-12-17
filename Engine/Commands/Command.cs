using System;
using Engine.Serialization;
using Engine.Session;

namespace Engine.Commands
{
    /// <summary>
    /// Base class for commands.
    /// </summary>
    public abstract class Command : ICommand
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
        public Player Player { get; set; }

        /// <summary>
        /// The type of the command.
        /// </summary>
        public Enum Type { get; private set; }

        #endregion

        #region Constructor

        protected Command(Enum type)
        {
            this.Type = type;
        }

        #endregion

        #region Serialization

        public abstract void Packetize(Packet packet);

        public abstract void Depacketize(Packet packet);

        #endregion

        #region Equality

        public virtual bool Equals(ICommand other)
        {
            return other != null && other.Type.Equals(this.Type) &&
                other.Player.Number == this.Player.Number;
        }

        #endregion
    }
}
