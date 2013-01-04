using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Engine.Serialization;

namespace Engine.Util
{
    /// <summary>
    /// Class that handles giving out unique ids, and releasing old ones so
    /// they may be reused.
    /// </summary>
    public sealed class IdManager : IPacketizable, ICopyable<IdManager>, IEnumerable<int>
    {
        #region Properties

        /// <summary>
        /// The number of IDs currently in use.
        /// </summary>
        public int Count
        {
            get { return _nextId - 1 - _reusableIds.Count; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The list of ids that were released and may be reused.
        /// </summary>
        [PacketizerIgnore]
        private SortedSet<int> _reusableIds = new SortedSet<int>();

        /// <summary>
        /// The next id we'll create if we have no reusable ids.
        /// </summary>
        private int _nextId = 1;

        #endregion

        #region Accessors

        /// <summary>
        /// Check if a given id is currently in use, i.e. given out by this
        /// manager.
        /// </summary>
        /// <param name="id">The id to check.</param>
        /// <returns></returns>
        public bool InUse(int id)
        {
            return id > 0 && id < _nextId && !_reusableIds.Contains(id);
        }

        /// <summary>
        /// Get a new unique id, relative to this manager.
        /// </summary>
        /// <returns>A new, unique id.</returns>
        public int GetId()
        {
            // Check if we have recycled ids, if so use one of those.
            if (_reusableIds.Count > 0)
            {
                // Get it and remove it from our list.
                var result = _reusableIds.Min;
                _reusableIds.Remove(result);
                return result;
            }
            else
            {
                // No, use a new id.
                return _nextId++;
            }
        }

        /// <summary>
        /// Releases an id this manager produced, so it may be reused.
        /// </summary>
        /// <param name="id">The id to release.</param>
        public void ReleaseId(int id)
        {
            // Check if this is an id we gave out and that's currently active.
            if (!InUse(id))
            {
                throw new ArgumentException("id");
            }
            // Check if the id is the last one we newly created.
            if (id == _nextId - 1)
            {
                // If so, clean up by not adding it, but decrementing the id
                // we'll give out next by one. Also check if after doing this
                // we can release some recycled ids for the same reason (them
                // now being the last newly created id.
                // Doing this keeps the memory use of this instance as low as
                // possible.
                while (id > 0 && id == _nextId - 1)
                {
                    _reusableIds.Remove(id);
                    --_nextId;
                    id = _reusableIds.Max;
                }
            }
            else
            {
                // Somewhere in the range of number we dealt out so far, mark
                // it as reusable.
                _reusableIds.Add(id);
            }
        }

        #endregion
        
        #region Serialization / Cloning

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>The packet after writing.</returns>
        [Packetize]
        public Packet Packetize(Packet packet)
        {
            packet.Write(_reusableIds.Count);
            foreach (var reusableId in _reusableIds)
            {
                packet.Write(reusableId);
            }

            return packet;
        }

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// after automatic depacketization has been performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        [PostDepacketize]
        public void Depacketize(Packet packet)
        {
            _reusableIds.Clear();
            var numReusableIds = packet.ReadInt32();
            for (var i = 0; i < numReusableIds; i++)
            {
                _reusableIds.Add(packet.ReadInt32());
            }
        }

        /// <summary>
        /// Creates a new copy of the object, that shares no mutable
        /// references with this instance.
        /// </summary>
        /// <returns>The copy.</returns>
        public IdManager NewInstance()
        {
            var copy = (IdManager)MemberwiseClone();

            _reusableIds = new SortedSet<int>();
            _nextId = 1;

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of the object, reusing the given object.
        /// </summary>
        /// <param name="into">The object to copy into.</param>
        /// <returns>The copy.</returns>
        public void CopyInto(IdManager into)
        {
            Debug.Assert(into != this);

            into._reusableIds.Clear();
            into._reusableIds.UnionWith(_reusableIds);
            into._nextId = _nextId;
        }

        #endregion

        #region IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<int> GetEnumerator()
        {
            for (var i = 1; i < _nextId; ++i)
            {
                if (InUse(i))
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
