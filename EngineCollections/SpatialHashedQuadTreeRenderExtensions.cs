using Engine.FarMath;
using Engine.Graphics;

namespace Engine.Collections
{
    /// <summary>
    /// Add extension methods for a QuadTree to allow rendering its nodes
    /// and entries.
    /// </summary>
    public static class SpatialHashedQuadTreeRenderExtensions
    {
        /// <summary>
        /// Renders a graphical representation of this tree's cells using the
        /// specified shape renderer.
        /// </summary>
        /// <param name="index">The tree to render.</param>
        /// <param name="shape">The shape renderer to paint with.</param>
        /// <param name="translation">The translation to apply to all draw
        /// operation.</param>
        public static void Draw<T>(this SpatialHashedQuadTree<T> index, AbstractShape shape, FarPosition translation)
        {
            foreach (var tree in index.GetTreeEnumerable())
            {
                tree.Item2.Draw(shape, tree.Item1 + translation);
            }
        }
    }
}
