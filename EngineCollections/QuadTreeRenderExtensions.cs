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
            var screenBounds = new FarRectangle(-5000, -5000, 10000, 10000);
            foreach (var node in quadTree.GetNodeEnumerable())
            {
                FarRectangle bounds = node.Item1;
                bounds.Offset(translation);
                var center = (Vector2)bounds.Center;

                if (screenBounds.Intersects(bounds) &&
                    !bounds.Contains(screenBounds))
                {
                    shape.SetCenter(center.X, center.Y);
                    shape.SetSize((int)bounds.Width - 1, (int)bounds.Height - 1);
                    shape.Draw();
                }

                // Check entries.
                foreach (var entry in node.Item2)
                {
                    bounds = quadTree[entry];
                    bounds.Offset(translation);
                    center = (Vector2)bounds.Center;

                    if (screenBounds.Intersects(bounds) &&
                        !bounds.Contains(screenBounds))
                    {
                        shape.SetCenter(center.X, center.Y);
                        shape.SetSize((int)bounds.Width, (int)bounds.Height);
                        shape.Draw();
                    }
                }
            }
        }
    }
}
