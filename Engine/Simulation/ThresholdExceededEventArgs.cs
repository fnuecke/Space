using System;

namespace Engine.Simulation
{
    /// <summary>
    /// Dispatched by the <see cref="TSS"/> to signal it had to roll
    /// back further than it could, meaning a new authoritative snapshot
    /// of the state has to be acquired.
    /// </summary>
    public class ThresholdExceededEventArgs : EventArgs
    {
        public ThresholdExceededEventArgs()
        {
        }
    }
}
