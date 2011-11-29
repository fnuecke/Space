using System;

namespace Engine.Simulation
{
    public class ThresholdExceededEventArgs : EventArgs
    {
        public ulong Frame { get; private set; }

        public ThresholdExceededEventArgs(ulong frame)
        {
            this.Frame = frame;
        }
    }
}
