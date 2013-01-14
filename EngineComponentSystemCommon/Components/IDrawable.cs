using Engine.FarMath;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    ///     Defines the interface for drawable components. These component's <see cref="Draw"/> function will be called by the
    ///     drawing system each frame. Drawables must also have an <see cref="Index"/> component, to allow the drawing system
    ///     to cull invisible entities.
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        ///     Draws the entity of the component. The implementation will generally depend on the type of entity (via the type of
        ///     <see cref="IDrawable"/> implementation).
        /// </summary>
        /// <param name="content">The content manager that may be used to load assets.</param>
        /// <param name="batch">The sprite batch that may be used to render textures.</param>
        /// <param name="translation">The translation to apply when drawing.</param>
        void Draw(ContentManager content, SpriteBatch batch, FarPosition translation);
    }
}