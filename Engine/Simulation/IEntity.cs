using System;
using Engine.Serialization;
using Engine.Util;

namespace Engine.Simulation
{
    /// <summary>
    /// Interface for world objects to be updated each frame.
    /// 
    /// IMPORTANT: implementations must perform a deep copy for
    /// all non-constant references (constant references may for
    /// example be things such as settings / read only values).
    /// </summary>
    public interface IEntity<TPlayerData>
        : IPacketizable<TPlayerData>, ICloneable, IHashable
        where TPlayerData : IPacketizable<TPlayerData>
    {
        /// <summary>
        /// The world (simulation) this object is associated with.
        /// </summary>
        IState<TPlayerData> State { get; set; }

        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        long UID { get; set; }

        /// <summary>
        /// Perform one simulation step. 
        /// </summary>
        void Update();
    }
}
