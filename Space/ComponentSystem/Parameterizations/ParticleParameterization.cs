using Engine.ComponentSystem.Parameterizations;
using ProjectMercury.Renderers;

namespace Space.ComponentSystem.Parameterizations
{
    /// <summary>
    /// Used by the particle system to query whether and where to spawn new
    /// particles.
    /// </summary>
    public sealed class ParticleParameterization : RendererParameterization
    {
        /// <summary>
        /// The particle renderer to use.
        /// </summary>
        public Renderer Renderer;
    }
}
