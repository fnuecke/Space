using System;
using System.Collections.Generic;
using Engine.Math;

namespace Engine.Network
{
    /// <summary>
    ///     Represents a type of traffic as tracked in the <c>ProtocolInfo</c> class.
    /// </summary>
    [Flags]
    public enum TrafficTypes
    {
        /// <summary>Protocol internal traffic, such as ACKs.</summary>
        Protocol = 1,

        /// <summary>Actual data packets, sent from a higher layer.</summary>
        Data = 2,

        /// <summary>Invalid data, only applies for incoming connections.</summary>
        Invalid = 4,

        /// <summary>Any type of packet.</summary>
        Any = Protocol | Data | Invalid
    }

    /// <summary>Represents some statistics for a protocol.</summary>
    public interface IProtocolInfo
    {
        /// <summary>Number of seconds that are being tracked in this protocol info.</summary>
        int HistoryLength { get; }

        /// <summary>Represents incoming traffic over a certain interval of time, tracked by type, and stored as number of bytes.</summary>
        LinkedList<Dictionary<TrafficTypes, int>> IncomingTraffic { get; }

        /// <summary>Represents outgoing traffic over a certain interval of time, tracked by type, and stored as number of bytes.</summary>
        LinkedList<Dictionary<TrafficTypes, int>> OutgoingTraffic { get; }

        /// <summary>A sampling over the size of incoming packets.</summary>
        ISampling<int> IncomingPacketSizes { get; }

        /// <summary>A sampling over the size of outgoing packets.</summary>
        ISampling<int> OutgoingPacketSizes { get; }

        /// <summary>A sampling over the compression rate of incoming packets.</summary>
        ISampling<double> IncomingPacketCompression { get; }

        /// <summary>A sampling over the compression rate of outgoing packets.</summary>
        ISampling<double> OutgoingPacketCompression { get; }
    }
}