﻿using System;
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
        /// Invalid data, only applies for incoming connections.
        /// </summary>
        Invalid = 4,

        /// <summary>
        /// Any type of packet.
        /// </summary>
        Any = Protocol | Data | Invalid
    }

    /// <summary>
    /// Represents some statistics for a protocol.
    /// </summary>
    public interface IProtocolInfo
    {
        /// <summary>
        /// Number of seconds that are being tracked in this protocol info.
        /// </summary>
        int HistoryLength { get; }

        /// <summary>
        /// Represents incoming traffic over a certain interval of time, tracked by type, and stored as number of bytes.
        /// </summary>
        LinkedList<Dictionary<TrafficType, int>> IncomingTraffic { get; }

        /// <summary>
        /// Represents outgoing traffic over a certain interval of time, tracked by type, and stored as number of bytes.
        /// </summary>
        LinkedList<Dictionary<TrafficType, int>> OutgoingTraffic { get; }
    }
}