using System;

namespace Engine.Simulation
{
    /// <summary>
    /// Interface for world objects to be updated each frame.
    /// 
    /// IMPORTANT: implementations must perform a deep copy for
    /// all non-constant references (constant references may for
    /// example be things such as settings / read only value).
    /// </summary>
    public interface ISteppable<TState> : ICloneable
    {

        /// <summary>
        /// The world (simulation) this object is associated with.
        /// </summary>
        TState State { get; set; }

        /// <summary>
        /// Perform one simulation step. 
        /// </summary>
        void Update();

    }
}
