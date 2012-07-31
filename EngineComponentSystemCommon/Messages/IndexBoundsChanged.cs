using Engine.Math;

namespace Engine.ComponentSystem.Common.Messages
{
    /// <summary>
    /// Sent by <code>Index</code> instances when their bounds change.
    /// </summary>
    public struct IndexBoundsChanged
    {
        /// <summary>
        /// The entity to which the indexable belongs.
        /// </summary>
        public int Entity;

        /// <summary>
        /// The new bounds of the indexable.
        /// </summary>
        public RectangleF Bounds;
    }
}
