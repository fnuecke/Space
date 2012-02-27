using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    public sealed class DropCommand : FrameCommand
    {
        
        #region Fields

        /// <summary>
        /// The position of the item to equip in the player's inventory.
        /// </summary>
        public int InventoryIndex;

        /// <summary>
        /// The Source of the command
        /// </summary>
        public Source Source;
        
        #endregion

        public DropCommand(int slot,Source source)
            :base(SpaceCommandType.DropItem)
        {
            InventoryIndex = slot;
            Source = source;
        }

        public DropCommand()
            :this(-1,Source.None)
        {
            
        }



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
                .Write(InventoryIndex)
                .Write((int)Source);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            InventoryIndex = packet.ReadInt32();
            Source = (Source)packet.ReadInt32();
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
                InventoryIndex == ((DropCommand)other).InventoryIndex &&
                Source == ((DropCommand)other).Source;
        }

        #endregion
    }
}
