using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// Used to signal a player should equip a specific item from his inventory.
    /// </summary>
    public sealed class EquipCommand : FrameCommand
    {
        #region Fields

        /// <summary>
        /// The position of the item to equip in the player's inventory.
        /// </summary>
        public int InventoryIndex;

        /// <summary>
        /// The slot to equip the item to.
        /// </summary>
        public int Slot;

        #endregion

        #region Constructor

        public EquipCommand(int inventoryPosition, int slot)
            : base(SpaceCommandType.Equip)
        {
            this.InventoryIndex = inventoryPosition;
            this.Slot = slot;
        }

        public EquipCommand()
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
                .Write(InventoryIndex)
                .Write(Slot);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            InventoryIndex = packet.ReadInt32();
            Slot = packet.ReadInt32();
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
                InventoryIndex == ((EquipCommand)other).InventoryIndex &&
                Slot == ((EquipCommand)other).Slot;
        }

        #endregion
    }
}
