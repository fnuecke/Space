using Engine.ComponentSystem.Spatial.Components;

namespace Engine.ComponentSystem.Spatial.Messages
{
    /// <summary>
    ///     Sent by <see cref="IIndexable"/> components when the index groups they define association to change.
    /// </summary>
    public struct IndexGroupsChanged
    {
        /// <summary>The index component for which the index groups changed.</summary>
        public IIndexable Component;

        /// <summary>The index grouped we no longer belong to.</summary>
        public ulong RemovedIndexGroups;

        /// <summary>The index groups we now belong to but did not before.</summary>
        public ulong AddedIndexGroups;
    }
}