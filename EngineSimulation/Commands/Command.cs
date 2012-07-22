using System;
using Engine.Serialization;

namespace Engine.Simulation.Commands
{
    /// <summary>
    /// Base class for commands.
    /// </summary>
    public abstract class Command : IPacketizable, IEquatable<Command>, IComparable<Command>
    {
        #region Fields

        /// <summary>
        /// Whether the command is signed (e.g. by a server) (<c>true</c>)
        /// or came from an untrustworthy source (e.g. another client) (<c>false</c>).
        /// </summary>
        public bool IsAuthoritative;

        /// <summary>
        /// The number of the player that issued the command.
        /// </summary>
        public int PlayerNumber = -1;

        /// <summary>
        /// The id of this command, specific to the player that created it.
        /// </summary>
        public int Id;

        /// <summary>
        /// The type of the command.
        /// </summary>
        public readonly Enum Type;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// </summary>
        /// <param name="type">The type of the command.</param>
        protected Command(Enum type)
        {
            Type = type;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public virtual Packet Packetize(Packet packet)
        {
            return packet
                .Write(IsAuthoritative)
                .Write(PlayerNumber)
                .Write(Id);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            IsAuthoritative = packet.ReadBoolean();
            PlayerNumber = packet.ReadInt32();
            Id = packet.ReadInt32();
        }

        #endregion

        #region Equality

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Command other)
        {
            return other != null &&
                other.Type.Equals(Type) &&
                other.PlayerNumber == PlayerNumber &&
                other.Id == Id;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(Command other)
        {
            // First sort by players, then by command id.
            if (PlayerNumber != other.PlayerNumber)
            {
                return PlayerNumber - other.PlayerNumber;
            }
            return Id - other.Id;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return GetType().Name + ", IsAuthoritative = " + IsAuthoritative + ", PlayerNumber = " + PlayerNumber + ", Type = " + Type;
        }

        #endregion
    }
}
