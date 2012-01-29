using Engine.ComponentSystem.Entities;
using Engine.Serialization;
using Engine.Simulation.Commands;

namespace Space.Simulation.Commands
{
    /// <summary>
    /// Command used to inject items from the void to players inventory.
    /// </summary>
    public sealed class AddItemCommand : FrameCommand
    {
        #region Fields

        /// <summary>
        /// The item to add to the inventory.
        /// </summary>
        public Entity Item;
        
        #endregion

        #region Constructor

        public AddItemCommand(Entity item)
            : base(SpaceCommandType.AddItem)
        {
            this.Item = item;
        }

        public AddItemCommand()
            : this(null)
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
                .Write(Item);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Item = packet.ReadPacketizable<Entity>();
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
            if (base.Equals(other))
            {
                // This is pretty lazy and slow, but it's only for debugging, so w/e.
                using (Packet item1 = new Packet())
                using (Packet item2 = new Packet())
                {
                    item1.Write(Item);
                    item2.Write(((AddItemCommand)other).Item);
                    return item1.Equals(item2);
                }
            }
            return false;
        }

        #endregion
    }
}
