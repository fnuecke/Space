using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// Command issued when a player wants to move an item in his inventory.
    /// </summary>
    public sealed class MoveItemCommand : FrameCommand
    {
        #region Fields
        
        /// <summary>
        /// The first inventory slot involved.
        /// </summary>
        public int FirstIndex;

        /// <summary>
        /// The second inventory slot involved.
        /// </summary>
        public int SecondIndex;

        #endregion

        #region Constructor
        
        public MoveItemCommand(int firstIndex, int secondIndex)
            : base(SpaceCommandType.MoveItem)
        {
            this.FirstIndex = firstIndex;
            this.SecondIndex = secondIndex;
        }

        public MoveItemCommand()
            : this(-1, -1)
        {
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
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(FirstIndex)
                .Write(SecondIndex);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            FirstIndex = packet.ReadInt32();
            SecondIndex = packet.ReadInt32();
        }

        #endregion

        #region Equals

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public override bool Equals(Command other)
        {
            return base.Equals(other) &&
                 FirstIndex == ((MoveItemCommand)other).FirstIndex &&
                 SecondIndex == ((MoveItemCommand)other).SecondIndex;
        }

        #endregion
    }
}
