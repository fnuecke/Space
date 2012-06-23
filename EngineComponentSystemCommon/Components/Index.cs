using System;
using Engine.ComponentSystem.Common.Messages;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Allows tracking a component with a position in the <c>IndexSystem</c>
    /// for quick nearest neighbor queries.
    /// </summary>
    public sealed class Index : Component
    {
        #region Properties

        /// <summary>
        /// The bit mask of the index group this component will belong to.
        /// There are a total of 64 separate groups, via the 64 bits in a
        /// ulong.
        /// </summary>
        public ulong IndexGroups
        {
            get { return _indexGroups; }
            set
            {
                if (value != _indexGroups)
                {
                    // Figure out which groups are new.
                    IndexGroupsChanged message;
                    message.Entity = Entity;
                    message.AddedIndexGroups = value & ~_indexGroups;
                    message.RemovedIndexGroups = _indexGroups & ~value;
                    _indexGroups = value;

                    if (Manager != null)
                    {
                        Manager.SendMessage(ref message);
                    }
                }
            }
        }

        #endregion

        #region Fields

        private ulong _indexGroups;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            _indexGroups = ((Index)other).IndexGroups;

            return this;
        }

        /// <summary>
        /// Initialize with the specified index groups.
        /// </summary>
        /// <param name="groups">The index groups.</param>
        public Index Initialize(ulong groups)
        {
            this.IndexGroups = groups;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            _indexGroups = 0;
        }

        #endregion

        #region Serialization / Hashing

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
                .Write(_indexGroups);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            _indexGroups = packet.ReadUInt64();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(BitConverter.GetBytes(_indexGroups));
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
            return base.ToString() + ", IndexGroups = " + _indexGroups;
        }

        #endregion
    }
}
