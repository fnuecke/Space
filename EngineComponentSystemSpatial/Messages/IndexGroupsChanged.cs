using Engine.ComponentSystem.Spatial.Components;

namespace Engine.ComponentSystem.Spatial.Messages
{
    /// <summary>
    ///     Sent by <see cref="IIndexable"/> components when the index id they define association to changes.
    /// </summary>
    public struct IndexGroupsChanged
    {
        /// <summary>The index component for which the index id changed.</summary>
        public IIndexable Component;

        /// <summary>The index we no longer belong to.</summary>
        public int OldIndexId;

        /// <summary>The index we now belong to.</summary>
        public int NewIndexId;
    }
}