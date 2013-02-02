namespace Engine.ComponentSystem.Messages
{
    /// <summary>Sent to perform a single update on systems.</summary>
    public struct Update
    {
        /// <summary>The current frame of simulation.</summary>
        public long Frame;
    }
}
