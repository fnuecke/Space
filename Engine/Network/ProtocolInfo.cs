using System;
using System.Collections.Generic;

namespace Engine.Network
{
    /// <summary>
    /// Represents a type of traffic as tracked in the <c>ProtocolInfo</c> class.
    /// </summary>
    [Flags]
    public enum TrafficType
    {
        /// <summary>
        /// Protocol internal traffic, such as acks.
        /// </summary>
        Protocol = 1,

        /// <summary>
        /// Actual data packets, sent from a higher layer.
        /// </summary>
        Data = 2,

        /// <summary>
        /// Any type of packet.
        /// </summary>
        Any = Protocol | Data
    }

    public sealed class ProtocolInfo
    {
        #region Properties

        /// <summary>
        /// Represents incoming traffic over a certain interval of time, tracked by type, and stored as number of bytes.
        /// </summary>
        public LinkedList<Dictionary<TrafficType, int>> IncomingTraffic { get { return incoming; } }

        /// <summary>
        /// Represents outgoing traffic over a certain interval of time, tracked by type, and stored as number of bytes.
        /// </summary>
        public LinkedList<Dictionary<TrafficType, int>> OutgoingTraffic { get { return outgoing; } }

        #endregion

        #region Fields

        /// <summary>
        /// Keeps track of incoming traffic over a certain interval of time.
        /// </summary>
        LinkedList<Dictionary<TrafficType, int>> incoming = new LinkedList<Dictionary<TrafficType, int>>();

        /// <summary>
        /// Keeps track of outgoing traffic over a certain interval of time.
        /// </summary>
        LinkedList<Dictionary<TrafficType, int>> outgoing = new LinkedList<Dictionary<TrafficType, int>>();

        /// <summary>
        /// The time we last added something to the history.
        /// </summary>
        private long currentSecond = (long)(new TimeSpan(DateTime.Now.Ticks).TotalSeconds);

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new protocol info object.
        /// </summary>
        /// <param name="history">the length of the history in seconds.</param>
        public ProtocolInfo(int history)
        {
            for (int i = 0; i < history; ++i)
            {
                var dict = new Dictionary<TrafficType, int>();
                dict[TrafficType.Data] = 0;
                dict[TrafficType.Protocol] = 0;
                dict[TrafficType.Any] = 0;
                incoming.AddLast(dict);

                dict = new Dictionary<TrafficType, int>();
                dict[TrafficType.Data] = 0;
                dict[TrafficType.Protocol] = 0;
                dict[TrafficType.Any] = 0;
                outgoing.AddLast(dict);
            }
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Add a new sample of incoming traffic for the current time.
        /// </summary>
        /// <param name="bytes">the number of bytes of traffic.</param>
        /// <param name="type">the type of traffic.</param>
        internal void Incoming(int bytes, TrafficType type)
        {
            UpdateLists();
            switch (type)
            {
                case TrafficType.Protocol:
                    incoming.First.Value[type] += bytes;
                    break;
                case TrafficType.Data:
                    incoming.First.Value[type] += bytes;
                    break;
                case TrafficType.Any:
                    throw new ArgumentException("Must be a specific type when adding.");
            }
            incoming.First.Value[TrafficType.Any] += bytes;
        }

        /// <summary>
        /// Add a new sample of outgoing traffic for the current time.
        /// </summary>
        /// <param name="bytes">the number of bytes of traffic.</param>
        /// <param name="type">the type of traffic.</param>
        internal void Outgoing(int bytes, TrafficType type)
        {
            UpdateLists();
            switch (type)
            {
                case TrafficType.Protocol:
                    outgoing.First.Value[type] += bytes;
                    break;
                case TrafficType.Data:
                    outgoing.First.Value[type] += bytes;
                    break;
                case TrafficType.Any:
                    throw new ArgumentException("Must be a specific type when adding.");
            }
            outgoing.First.Value[TrafficType.Any] += bytes;
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Helper method, loops list as necessary.
        /// </summary>
        private void UpdateLists()
        {
            long nowSecond = (long)(new TimeSpan(DateTime.Now.Ticks).TotalSeconds);
            for (long createIdx = currentSecond; currentSecond < nowSecond; ++currentSecond)
            {
                Dictionary<TrafficType, int> dictInc = incoming.Last.Value;
                Dictionary<TrafficType, int> dictOut = outgoing.Last.Value;
                incoming.RemoveLast();
                outgoing.RemoveLast();
                foreach (var key in new List<TrafficType>(dictInc.Keys))
                {
                    dictInc[key] = 0;
                }
                foreach (var key in new List<TrafficType>(dictOut.Keys))
                {
                    dictOut[key] = 0;
                }
                incoming.AddFirst(dictInc);
                outgoing.AddFirst(dictOut);
            }
        }

        #endregion
    }
}
