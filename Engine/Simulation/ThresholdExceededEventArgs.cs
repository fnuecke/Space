using System;

namespace Engine.Simulation
{
    public class ThresholdExceededEventArgs : EventArgs
    {
        public long Frame { get; private set; }

        public ThresholdExceededEventArgs(long frame)
        {
            this.Frame = frame;
        }
    }
}
