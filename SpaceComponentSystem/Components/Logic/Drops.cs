using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Tracks what items a unit may drop on death, via the item pool id to
    /// draw items from.
    /// </summary>
    public sealed class Drops : Component
    {
        #region Fields

        /// <summary>
        /// The logical name of the item pool to draw items from when the unit
        /// dies.
        /// </summary>
        public string ItemPool;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            ItemPool = ((Drops)other).ItemPool;

            return this;
        }

        /// <summary>
        /// Initialize with the specified item pool.
        /// </summary>
        /// <param name="itemPool">The item pool.</param>
        public Drops Initialize(string itemPool)
        {
            ItemPool = itemPool;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            ItemPool = null;
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
                .Write(ItemPool);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            ItemPool = packet.ReadString();
        }

        #endregion
    }
}
