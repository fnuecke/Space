using Engine.ComponentSystem.Spatial.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.ComponentSystem.Spatial.Messages
{
    /// <summary>
    ///     Sent by <see cref="IIndexable"/> components when their bounds change.
    /// </summary>
    public struct IndexBoundsChanged
    {
        /// <summary>The entity to which the indexable belongs.</summary>
        public IIndexable Component;

        /// <summary>The new world bounds of the indexable.</summary>
        public WorldBounds Bounds;

        /// <summary>The amount by which the component moved in the update.</summary>
        public Vector2 Delta;
    }
}