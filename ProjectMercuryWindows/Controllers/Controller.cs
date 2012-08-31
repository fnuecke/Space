using Engine.FarMath;

namespace ProjectMercury.Controllers
{
    using Microsoft.Xna.Framework;

    /// <summary>
    /// Defines the abstract base class for a controller.
    /// </summary>
    public abstract class Controller
    {
        /// <summary>
        /// Gets or sets which particle effect the controller is assigned to.
        /// </summary>
        public ParticleEffect ParticleEffect { get; internal set; }

        /// <summary>
        /// Called by the particle effect when it is triggered.
        /// </summary>
        /// <param name="position">The desired position of the trigger.</param>
        /// <param name="impulse">The impulse.</param>
        /// <param name="rotation">The rotation.</param>
        /// <param name="scale">The scale.</param>
        /// <remarks>
        /// This method should not be called directly, the ParticleEffect class
        /// will defer control to this method when the controller is assigned.
        /// </remarks>
        protected internal virtual void Trigger(ref FarPosition position, ref Vector2 impulse, float rotation = 0.0f, float scale = 1.0f)
        {
            if (this.ParticleEffect != null)
                for (int i = 0; i < this.ParticleEffect.Count; i++)
                    this.ParticleEffect[i].Trigger(ref position, ref impulse, rotation, scale);
        }

        /// <summary>
        /// Called by the particle effect when it is updated.
        /// </summary>
        /// <param name="deltaSeconds">Elapsed time in whole and fractional seconds.</param>
        /// <remarks>
        /// This method should not be called directly, the ParticleEffect class
        /// will defer control to this method when the controller is assigned.
        /// </remarks>
        protected internal virtual void Update(float deltaSeconds)
        {
            if (this.ParticleEffect != null)
                for (int i = 0; i < this.ParticleEffect.Count; i++)
                    this.ParticleEffect[i].Update(deltaSeconds);
        }
    }
}