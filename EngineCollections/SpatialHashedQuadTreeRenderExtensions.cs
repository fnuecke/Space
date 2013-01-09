using Engine.Graphics;
using Microsoft.Xna.Framework;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
#if FARMATH
using Engine.Collections;
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
    /// <summary>Add extension methods for a QuadTree to allow rendering its nodes and entries.</summary>
    public static class SpatialHashedQuadTreeRenderExtensions
    {
        /// <summary>Renders a graphical representation of this tree's cells using the specified shape renderer.</summary>
        /// <param name="index">The tree to render.</param>
        /// <param name="shape">The shape renderer to paint with.</param>
        /// <param name="translation">The translation to apply to all draw operation.</param>
        public static void Draw<T>(this SpatialHashedQuadTree<T> index, AbstractShape shape, TPoint translation)
        {
            foreach (var tree in index.GetTreeEnumerable())
            {
// ReSharper disable RedundantCast Necessary for FarCollections.
                tree.Item2.Draw(shape, (Vector2) (tree.Item1 + translation));
// ReSharper restore RedundantCast
            }
        }
    }
}