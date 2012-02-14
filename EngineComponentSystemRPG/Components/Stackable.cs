using Engine.ComponentSystem.Components;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    /// <summary>
    /// Makes an item stackable. Items with the same group id can be merged
    /// into a single stack.
    /// </summary>
    public sealed class Stackable : AbstractComponent
    {
        #region Fields

        /// <summary>
        /// The stackable group id used to check if two stacks can be merged
        /// into one larger stack.
        /// </summary>
        public int GroupId;

        /// <summary>
        /// The current number of items in the stack.
        /// </summary>
        public int Count;

        /// <summary>
        /// The maximum number of items that can be in a single stack.
        /// </summary>
        public int MaxCount;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new stack of size one from the specified parameters.
        /// </summary>
        /// <param name="groupId">The group id for this stackable.</param>
        /// <param name="maxCount">The maximum number of items that can be
        /// merged into a single stack.</param>
        public Stackable(int groupId, int maxCount)
        {
            this.GroupId = groupId;
            this.Count = 1;
            this.MaxCount = maxCount;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        public Stackable()
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
                .Write(GroupId)
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

            GroupId = packet.ReadInt32();
            Count = packet.ReadInt32();
            MaxCount = packet.ReadInt32();
        }

        #endregion

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Stackable)base.DeepCopy(into);

            if (copy == into)
            {
                copy.GroupId = GroupId;
                copy.Count = Count;
                copy.MaxCount = MaxCount;
            }

            return copy;
        }

        #endregion
    }
}
