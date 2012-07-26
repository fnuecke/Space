using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    internal sealed class DropCommand : FrameCommand
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

        #region Constructor

        public DropCommand(int slot, Source source)
            : base(SpaceCommandType.DropItem)
        {
            InventoryIndex = slot;
            Source = source;
        }

        public DropCommand()
            : this(-1, Source.None)
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
    }
}
