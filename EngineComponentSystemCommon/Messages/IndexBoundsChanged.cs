using Engine.ComponentSystem.Common.Components;
using Engine.FarMath;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    ///     Sent by <see cref="Index"/> components when their bounds change.
    /// </summary>
    public struct IndexBoundsChanged
    {
        /// <summary>The entity to which the indexable belongs.</summary>
        public IIndexable Component;

        /// <summary>The new bounds of the indexable.</summary>
        public FarRectangle Bounds;
    }
}