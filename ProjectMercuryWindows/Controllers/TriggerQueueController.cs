using System;
using Engine.FarMath;

namespace ProjectMercury.Controllers
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// Defines a simple controller which queues triggers rather than applying them immediately.
    /// The next queued trigger is applied when the particle effect has an active particle count of zero.
    /// </summary>
    public sealed class TriggerQueueController : Controller
    {
        /// <summary>
        /// Gets the queued triggers which need to be applied to the particle effect.
        /// </summary>
        public Queue<Tuple<FarPosition, Vector2, float, float>> QueuedTriggers { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerQueueController"/> class.
        /// </summary>
        public TriggerQueueController()
        {
            this.QueuedTriggers = new Queue<Tuple<FarPosition, Vector2, float, float>>();
        }

        /// <summary>
        /// Called by the particle effect when it is triggered.
        /// </summary>
        /// <param name="position">The desired position of the trigger.</param>
        /// <param name="impulse"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        /// <remarks>
        /// This method should not be called directly, the ParticleEffect class
        /// will defer control to this method when the controller is assigned.
        /// </remarks>
        protected internal override void Trigger(ref FarPosition position, ref Vector2 impulse, float rotation = 0.0f, float scale = 1.0f)
        {
            this.QueuedTriggers.Enqueue(Tuple.Create(position, impulse, rotation, scale));
        }

        /// <summary>
        /// Called by the particle effect when it is updated.
        /// </summary>
        /// <param name="deltaSeconds">Elapsed time in whole and fractional seconds.</param>
        /// <remarks>This method should not be called directly, the ParticleEffect class
        /// will defer control to this method when the controller is assigned.</remarks>
        protected internal override void Update(float deltaSeconds)
        {
            if (base.ParticleEffect != null)
                if (this.QueuedTriggers.Count > 0)
                    if (base.ParticleEffect.ActiveParticlesCount == 0)
                    {
                        var triggerInfo = this.QueuedTriggers.Dequeue();

                        for (int i = 0; i < this.ParticleEffect.Count; i++)
                            this.ParticleEffect[i].Trigger(triggerInfo.Item1, triggerInfo.Item2, triggerInfo.Item3, triggerInfo.Item4);
                    }

            base.Update(deltaSeconds);
        }
    }
}