using System;
using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Interface for world objects to be updated each frame.
    /// 
    /// IMPORTANT: implementations must perform a deep copy for
    /// all non-constant references (constant references may for
    /// example be things such as settings / read only values).
    /// </summary>
    public interface ISteppable<TState, TSteppable, TCommandType, TPlayerData> : ICloneable, IPacketizable
        where TState : IState<TState, TSteppable, TCommandType, TPlayerData>
        where TSteppable : ISteppable<TState, TSteppable, TCommandType, TPlayerData>
        where TCommandType : struct
        where TPlayerData : IPacketizable
    {
        /// <summary>
        /// The world (simulation) this object is associated with.
        /// </summary>
        TState State { get; set; }

        /// <summary>
        /// A globally unique id for this object.
        /// </summary>
        long UID { get; }

        /// <summary>
        /// Perform one simulation step. 
        /// </summary>
        void Update();
    }
}
