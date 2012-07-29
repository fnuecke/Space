using Engine.Graphics;
using Microsoft.Xna.Framework;

namespace Engine.Collections
{
    /// <summary>
    /// Add extension methods for a QuadTree to allow rendering its nodes
    /// and entries.
    /// </summary>
    public static class QuadTreeRenderExtensions
    {
        /// <summary>
        /// Renders a graphical representation of this tree's cells using the
        /// specified shape renderer.
        /// </summary>
        /// <param name="quadTree">The tree to render.</param>
        /// <param name="shape">The shape renderer to paint with.</param>
        /// <param name="translation">The translation to apply to all draw
        /// operation.</param>
        public static void Draw<T>(this QuadTree<T> quadTree, AbstractShape shape, Vector2 translation)
        {
            foreach (var node in quadTree.GetNodeEnumerable())
            {
                var nodeBounds = node.Item1;
                shape.SetCenter(translation.X + nodeBounds.X + (nodeBounds.Width >> 1),
                                translation.Y + nodeBounds.Y + (nodeBounds.Height >> 1));
                shape.SetSize(nodeBounds.Width - 1, nodeBounds.Height - 1);
                shape.Draw();

                foreach (var entry in node.Item2)
                {
                    var entryBounds = entry.Item1;
                    shape.SetCenter(translation.X + entryBounds.X + (entryBounds.Width >> 1),
                                    translation.Y + entryBounds.Y + (entryBounds.Height >> 1));
                    shape.SetSize(entryBounds.Width, entryBounds.Height);
                    shape.Draw();
                }
            }
        }
    }
}
