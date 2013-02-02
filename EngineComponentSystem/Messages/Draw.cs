namespace Engine.ComponentSystem.Messages
{
    /// <summary>Sent to perform a render pass.</summary>
    public struct Draw
    {
        /// <summary>The current frame of simulation.</summary>
        public long Frame;

        /// <summary>The total milliseconds elapsed since the last draw message.</summary>
        public float ElapsedMilliseconds;
    }
}
