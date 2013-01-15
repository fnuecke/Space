using Engine.ComponentSystem.Components;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>
    /// Interface for components with a transformation, i.e. a position and rotation.
    /// </summary>
    public interface ITransform : IComponent
    {
        /// <summary>Gets or set the current position of the entity.</summary>
        WorldPoint Position { get; set; }

        /// <summary>Gets or sets the current angle/orientation of the entity.</summary>
        float Angle { get; set; }
    }
}
