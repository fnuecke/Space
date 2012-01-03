using Microsoft.Xna.Framework;

namespace Space.ComponentSystem.Parameterizations
{
    /// <summary>
    /// Used by the particle system to query whether and where to spawn new
    /// particles.
    /// </summary>
    public sealed class ParticleParameterization
    {
        public Engine.ComponentSystem.Systems.ComponentSystemUpdateType UpdateType;
        public Matrix Matrix;
        public Microsoft.Xna.Framework.Content.ContentManager Content;
        public ProjectMercury.Renderers.Renderer Renderer;
    }
}
