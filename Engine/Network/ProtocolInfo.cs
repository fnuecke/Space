using System;
using System.Collections.Generic;

namespace Engine.Network
{
    /// <summary>
    /// Represents some statistics for a protocol.
    /// </summary>
    internal sealed class ProtocolInfo : IProtocolInfo
    {
        #region Properties

        /// <summary>
        /// Number of seconds that are being tracked in this protocol info.
        /// </summary>
        public int HistoryLength { get; private set; }

        /// <summary>
        /// Represents incoming traffic over a certain interval of time, tracked by type, and stored as number of bytes.
        /// </summary>
        public LinkedList<Dictionary<TrafficTypes, int>> IncomingTraffic
        {
            get
            {
                UpdateLists();
                return incoming;
            }
        }

        /// <summary>
        /// Represents outgoing traffic over a certain interval of time, tracked by type, and stored as number of bytes.
        /// </summary>
        public LinkedList<Dictionary<TrafficTypes, int>> OutgoingTraffic
        {
            get
            {
                UpdateLists();
                return outgoing;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Keeps track of incoming traffic over a certain interval of time.
        /// </summary>
        private LinkedList<Dictionary<TrafficTypes, int>> incoming = new LinkedList<Dictionary<TrafficTypes, int>>();

        /// <summary>
        /// Keeps track of outgoing traffic over a certain interval of time.
        /// </summary>
        private LinkedList<Dictionary<TrafficTypes, int>> outgoing = new LinkedList<Dictionary<TrafficTypes, int>>();

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
            HistoryLength = history;
            for (int i = 0; i < history; ++i)
            {
                var dict = new Dictionary<TrafficTypes, int>();
                dict[TrafficTypes.Data] = 0;
                dict[TrafficTypes.Protocol] = 0;
                dict[TrafficTypes.Invalid] = 0;
                dict[TrafficTypes.Any] = 0;
                incoming.AddLast(dict);

                dict = new Dictionary<TrafficTypes, int>();
                dict[TrafficTypes.Data] = 0;
                dict[TrafficTypes.Protocol] = 0;
                dict[TrafficTypes.Any] = 0;
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
        internal void Incoming(int bytes, TrafficTypes type)
        {
            UpdateLists();
            switch (type)
            {
                case TrafficTypes.Protocol:
                    incoming.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Data:
                    incoming.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Invalid:
                    incoming.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Any:
                    throw new ArgumentException("Must be a specific type when adding.");
            }
            incoming.First.Value[TrafficTypes.Any] += bytes;
        }

        /// <summary>
        /// Add a new sample of outgoing traffic for the current time.
        /// </summary>
        /// <param name="bytes">the number of bytes of traffic.</param>
        /// <param name="type">the type of traffic.</param>
        internal void Outgoing(int bytes, TrafficTypes type)
        {
            UpdateLists();
            switch (type)
            {
                case TrafficTypes.Protocol:
                    outgoing.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Data:
                    outgoing.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Any:
                    throw new ArgumentException("Must be a specific type when adding.");
            }
            outgoing.First.Value[TrafficTypes.Any] += bytes;
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
                Dictionary<TrafficTypes, int> dictInc = incoming.Last.Value;
                Dictionary<TrafficTypes, int> dictOut = outgoing.Last.Value;
                incoming.RemoveLast();
                outgoing.RemoveLast();
                foreach (var key in new List<TrafficTypes>(dictInc.Keys))
                {
                    dictInc[key] = 0;
                }
                foreach (var key in new List<TrafficTypes>(dictOut.Keys))
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
