using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Makes an item stackable. Items with the same group id can be merged
    /// into a single stack.
    /// </summary>
    public sealed class Stackable : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The current number of items in the stack.
        /// </summary>
        public int Count;

        /// <summary>
        /// The maximum number of items that can be in a single stack.
        /// </summary>
        public int MaxCount;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherStackable = (Stackable)other;
            Count = otherStackable.Count;
            MaxCount = otherStackable.MaxCount;

            return this;
        }

        /// <summary>
        /// Initialize with the specified parameters.
        /// </summary>
        /// <param name="maxCount">The maximum number of items that can be
        /// merged into a single stack.</param>
        public Stackable Initialize(int maxCount)
        {
            this.Count = 1;
            this.MaxCount = maxCount;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Count = 0;
            MaxCount = 0;
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
                .Write(Count)
                .Write(MaxCount);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Count = packet.ReadInt32();
            MaxCount = packet.ReadInt32();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(Count);
            hasher.Put(MaxCount);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Count=" + Count + ", MaxCount=" + MaxCount;
        }

        #endregion
    }
}
