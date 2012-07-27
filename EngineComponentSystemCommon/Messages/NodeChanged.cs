namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    /// Sent by the NodeSystem when an entity gets transferred to a different node.
    /// </summary>
    public struct NodeChanged
    {
        /// <summary>
        /// The entity for which the node changed.
        /// </summary>
        public int Entity;

        /// <summary>
        /// The previous node before the change.
        /// </summary>
        public ulong PreviousNode;

        /// <summary>
        /// The current node after the change.
        /// </summary>
        public ulong CurrentNode;
    }
}
