using System;
using System.Collections.Generic;
using Engine.Serialization;

namespace Engine.Util
{
    /// <summary>
    /// Class that handles giving out unique ids, and releasing old ones so
    /// they may be reused.
    /// </summary>
    public class IdManager : IPacketizable, ICloneable
    {
        #region Fields
        
        /// <summary>
        /// The list of ids that were released and may be reused.
        /// </summary>
        private SortedSet<int> _reusableIds = new SortedSet<int>();

        /// <summary>
        /// The next id we'll create if we have no reusable ids.
        /// </summary>
        private int _nextId = 1;

        #endregion

        /// <summary>
        /// Check if a given id is currently in use, i.e. given out by this
        /// manager.
        /// </summary>
        /// <param name="id">The id to check.</param>
        /// <returns></returns>
        public bool InUse(int id)
        {
            return id < _nextId && !_reusableIds.Contains(id);
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
                int result = _reusableIds.Min;
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
                while (id == _nextId - 1)
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

        #region Serialization / Cloning

        public Packet Packetize(Packet packet)
        {
            packet
                .Write(_nextId)
                .Write(_reusableIds.Count);
            foreach (var reusableId in _reusableIds)
            {
                packet.Write(reusableId);
            }

            return packet;
        }

        public void Depacketize(Packet packet)
        {
            _nextId = packet.ReadInt32();

            _reusableIds.Clear();
            var numReusableIds = packet.ReadInt32();
            for (int i = 0; i < numReusableIds; i++)
            {
                _reusableIds.Add(packet.ReadInt32());
            }
        }

        public object Clone()
        {
            var copy = (IdManager)MemberwiseClone();
            copy._reusableIds = new SortedSet<int>(_reusableIds);
            return copy;
        }

        #endregion
    }
}
