using Engine.ComponentSystem.Components;
using Engine.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>
    ///     Defines the interface for drawable components. These component's <see cref="Draw"/> function will be called by the
    ///     drawing system each frame. If drawables also have an <see cref="IIndexable"/> component, the drawing system can
    ///     cull cull invisible entities automatically.
    /// </summary>
    public interface IDrawable : IComponent
    {
        /// <summary>The area this drawable needs to render itself.</summary>
        RectangleF Bounds { get; }

        /// <summary>
        ///     Draws the entity of the component. The implementation will generally depend on the type of entity (via the type of
        ///     <see cref="IDrawable"/> implementation).
        /// </summary>
        /// <param name="batch">The sprite batch that may be used to render textures.</param>
        /// <param name="position">The position at which to draw. This already includes camera and object position.</param>
        /// <param name="angle">The angle at which to draw. This includes the object angle.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="effects">The effects.</param>
        /// <param name="layerDepth">The base layer depth to use, used for tie breaking.</param>
        void Draw(SpriteBatch batch, Vector2 position, float angle, float scale, SpriteEffects effects, float layerDepth);

        /// <summary>Called when the component should (re)load any assets.</summary>
        /// <param name="content">The content manager to use.</param>
        /// <param name="graphics">The graphics device to render to.</param>
        void LoadContent(ContentManager content, IGraphicsDeviceService graphics);
    }
}