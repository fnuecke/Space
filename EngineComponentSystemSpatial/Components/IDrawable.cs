using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>
    ///     Defines the interface for drawable components. These component's <see cref="Draw"/> function will be called by the
    ///     drawing system each frame. If drawables also have an <see cref="IIndexable"/> component, the drawing system can cull
    ///     cull invisible entities automatically.
    /// </summary>
    public interface IDrawable : IComponent
    {
        /// <summary>
        ///     Draws the entity of the component. The implementation will generally depend on the type of entity (via the type of
        ///     <see cref="IDrawable"/> implementation).
        /// </summary>
        /// <param name="batch">The sprite batch that may be used to render textures.</param>
        /// <param name="translation">The translation to apply when drawing.</param>
        void Draw(SpriteBatch batch, WorldPoint translation);

        /// <summary>Called when the component should (re)load any assets.</summary>
        /// <param name="content">The content manager to use.</param>
        /// <param name="graphics">The graphics device to render to.</param>
        void LoadContent(ContentManager content, IGraphicsDeviceService graphics);
    }
}