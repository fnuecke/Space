using Engine.FarMath;
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
        public static void Draw<T>(this QuadTree<T> quadTree, AbstractShape shape, FarPosition translation)
        {
            foreach (var node in quadTree.GetNodeEnumerable())
            {
                var nodeBounds = node.Item1;
                var center = nodeBounds.Center;
                var transformedCenter = (Vector2)(center + translation);
                shape.SetCenter(transformedCenter.X, transformedCenter.Y);
                shape.SetSize((int)nodeBounds.Width - 1, (int)nodeBounds.Height - 1);
                shape.Draw();

                foreach (var entry in node.Item2)
                {
                    var bounds = quadTree[entry];
                    center = bounds.Center;
                    transformedCenter = (Vector2)(center + translation);
                    shape.SetCenter(transformedCenter.X, transformedCenter.Y);
                    shape.SetSize((int)bounds.Width, (int)bounds.Height);
                    shape.Draw();
                }
            }
        }
    }
}
