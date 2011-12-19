using System;
using Engine.Serialization;

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
        /// The number of the player that issued the command.
        /// </summary>
        public int PlayerNumber { get; set; }

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

        public virtual void Packetize(Packet packet)
        {
            packet.Write(PlayerNumber);
        }

        public virtual void Depacketize(Packet packet)
        {
            PlayerNumber = packet.ReadInt32();
        }

        #endregion

        #region Equality

        public virtual bool Equals(ICommand other)
        {
            return other != null && other.Type.Equals(this.Type) &&
                other.PlayerNumber == this.PlayerNumber;
        }

        #endregion
    }
}
