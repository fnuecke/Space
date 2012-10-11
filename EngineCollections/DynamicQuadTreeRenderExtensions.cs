using Engine.Graphics;
using Microsoft.Xna.Framework;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
#if FARMATH
using TPoint = Engine.FarMath.FarPosition;
using TSingle = Engine.FarMath.FarValue;
using TRectangle = Engine.FarMath.FarRectangle;
#else
using TPoint = Microsoft.Xna.Framework.Vector2;
using TSingle = System.Single;
using TRectangle = Engine.Math.RectangleF;
#endif

#if FARMATH
namespace Engine.FarCollections
#else
namespace Engine.Collections
#endif
{
    /// <summary>
    /// Add extension methods for a QuadTree to allow rendering its nodes
    /// and entries.
    /// </summary>
    public static class DynamicQuadTreeRenderExtensions
    {
        /// <summary>
        /// Renders a graphical representation of this tree's cells using the
        /// specified shape renderer.
        /// </summary>
        /// <param name="tree">The tree to render.</param>
        /// <param name="shape">The shape renderer to paint with.</param>
        /// <param name="translation">The translation to apply to all draw
        /// operation.</param>
        public static void Draw<T>(this DynamicQuadTree<T> tree, AbstractShape shape, TPoint translation)
        {
            var screenBounds = new TRectangle(-5000, -5000, 10000, 10000);
            foreach (var node in tree.GetNodeEnumerable())
            {
                var bounds = node.Item1;
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
                    bounds = tree[entry];
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
