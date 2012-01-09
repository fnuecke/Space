using System;
using System.Collections.Generic;
using Engine.Util;

namespace Engine.Network
{
    /// <summary>
    /// Represents some statistics for a protocol.
    /// </summary>
    public sealed class ProtocolInfo : IProtocolInfo
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
                return inTraffic;
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
                return outTraffic;
            }
        }

        /// <summary>
        /// Sampling of the sizes of packet sizes of incoming packets.
        /// </summary>
        public ISampling<int> IncomingPacketSizes { get { return inPacketSizes; } }

        /// <summary>
        /// Sampling of the sizes of packet sizes of outgoing packets.
        /// </summary>
        public ISampling<int> OutgoingPacketSizes { get { return outPacketSizes; } }

        /// <summary>
        /// Sampling of the compression ratio of received packets.
        /// </summary>
        public ISampling<double> IncomingPacketCompression { get { return inPacketCompression; } }

        /// <summary>
        /// Sampling of the compression ratio of sent packets.
        /// </summary>
        public ISampling<double> OutgoingPacketCompression { get { return outPacketCompression; } }

        #endregion

        #region Fields

        /// <summary>
        /// Keeps track of incoming traffic over a certain interval of time.
        /// </summary>
        private LinkedList<Dictionary<TrafficTypes, int>> inTraffic = new LinkedList<Dictionary<TrafficTypes, int>>();

        /// <summary>
        /// Keeps track of outgoing traffic over a certain interval of time.
        /// </summary>
        private LinkedList<Dictionary<TrafficTypes, int>> outTraffic = new LinkedList<Dictionary<TrafficTypes, int>>();

        /// <summary>
        /// The time we last added something to the history.
        /// </summary>
        private long currentSecond = (long)(new TimeSpan(DateTime.Now.Ticks).TotalSeconds);

        private IntSampling inPacketSizes = new IntSampling(100);
        private IntSampling outPacketSizes = new IntSampling(100);
        private DoubleSampling inPacketCompression = new DoubleSampling(100);
        private DoubleSampling outPacketCompression = new DoubleSampling(100);

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
                inTraffic.AddLast(dict);

                dict = new Dictionary<TrafficTypes, int>();
                dict[TrafficTypes.Data] = 0;
                dict[TrafficTypes.Protocol] = 0;
                dict[TrafficTypes.Any] = 0;
                outTraffic.AddLast(dict);
            }
        }

        #endregion

        #region Internals

        /// <summary>
        /// Add a new sample of incoming traffic for the current time.
        /// </summary>
        /// <param name="bytes">the number of bytes of traffic.</param>
        /// <param name="type">the type of traffic.</param>
        public void PutIncomingTraffic(int bytes, TrafficTypes type)
        {
            UpdateLists();
            switch (type)
            {
                case TrafficTypes.Protocol:
                    inTraffic.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Data:
                    inTraffic.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Invalid:
                    inTraffic.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Any:
                    throw new ArgumentException("Must be a specific type when adding.");
            }
            inTraffic.First.Value[TrafficTypes.Any] += bytes;
        }

        /// <summary>
        /// Add a new sample of outgoing traffic for the current time.
        /// </summary>
        /// <param name="bytes">the number of bytes of traffic.</param>
        /// <param name="type">the type of traffic.</param>
        public void PutOutgoingTraffic(int bytes, TrafficTypes type)
        {
            UpdateLists();
            switch (type)
            {
                case TrafficTypes.Protocol:
                    outTraffic.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Data:
                    outTraffic.First.Value[type] += bytes;
                    break;
                case TrafficTypes.Any:
                    throw new ArgumentException("Must be a specific type when adding.");
            }
            outTraffic.First.Value[TrafficTypes.Any] += bytes;
        }

        public void PutIncomingPacketSize(int size)
        {
            inPacketSizes.Put(size);
        }

        public void PutOutgoingPacketSize(int size)
        {
            outPacketSizes.Put(size);
        }

        public void PutIncomingPacketCompression(double ratio)
        {
            inPacketCompression.Put(ratio);
        }

        public void PutOutgoingPacketCompression(double ratio)
        {
            outPacketCompression.Put(ratio);
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
                Dictionary<TrafficTypes, int> dictInc = inTraffic.Last.Value;
                Dictionary<TrafficTypes, int> dictOut = outTraffic.Last.Value;
                inTraffic.RemoveLast();
                outTraffic.RemoveLast();
                foreach (var key in new List<TrafficTypes>(dictInc.Keys))
                {
                    dictInc[key] = 0;
                }
                foreach (var key in new List<TrafficTypes>(dictOut.Keys))
                {
                    dictOut[key] = 0;
                }
                inTraffic.AddFirst(dictInc);
                outTraffic.AddFirst(dictOut);
            }
        }

        #endregion
    }
}
