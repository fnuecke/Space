using System;
using Engine.Serialization;

namespace Engine.Simulation.Commands
{
    /// <summary>
    /// Base class for commands.
    /// </summary>
    public abstract class Command : IPacketizable, IEquatable<Command>
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
        public int PlayerNumber;

        /// <summary>
        /// The type of the command.
        /// </summary>
        public readonly Enum Type;

        #endregion

        #region Constructor

        protected Command(Enum type)
        {
            this.Type = type;
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
            return packet.Write(PlayerNumber);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public virtual void Depacketize(Packet packet)
        {
            PlayerNumber = packet.ReadInt32();
        }

        #endregion

        #region Equality

        public virtual bool Equals(Command other)
        {
            return other != null && other.Type.Equals(this.Type) &&
                other.PlayerNumber == this.PlayerNumber;
        }

        #endregion
    }
}
