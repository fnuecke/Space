using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;
using ProjectMercury.Emitters;

namespace Space.ComponentSystem.Components
{
    /// <summary>
    /// Component responsible for rendering a ship's thruster exhaust effects.
    /// </summary>
    public sealed class ThrusterEffect : Effect
    {
        #region Constructor

        public ThrusterEffect(string effectName)
            : base(effectName)
        {
            // Draw beneath ships but above other stuff.
            DrawOrder = 40;
        }

        public ThrusterEffect()
            : this(string.Empty)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the rotation for all emitters, then delegates to the
        /// parent for actual particle generation.
        /// </summary>
        /// <param name="parameterization"></param>
        public override void Update(object parameterization)
        {
            // Enable self if some acceleration is set.
            var acceleration = Entity.GetComponent<Acceleration>();
            Emitting = (acceleration != null && acceleration.Value != Vector2.Zero);

            // Do we have an effect yet?
            if (_effect != null)
            {
                // Yes, get transform.
                var transform = Entity.GetComponent<Transform>();
                if (transform != null)
                {
                    // Get the current rotation and velocity.
                    var rotation = transform.Rotation + (float)System.Math.PI;
                    var velocity = Entity.GetComponent<Velocity>();
                    foreach (var emitter in _effect)
                    {
                        // Adjust the rotation for all directed emitters.
                        var cone = emitter as ConeEmitter;
                        if (cone != null)
                        {
                            cone.Direction = rotation;
                        }

                        // Adjust the release impulse based on our velocity.
                        if (velocity != null)
                        {
                            // TODO hard-coded updates / sec - 1, do this in a more generic fashion.
                            emitter.ReleaseImpulse = velocity.Value * 59;
                            // HACK: not sure why we need this, but otherwise
                            // the actual point of emission "wanders".
                            emitter.TriggerOffset = -velocity.Value;
                        }
                    }
                }
            }

            base.Update(parameterization);
        }

        #endregion

        #region Copying

        protected override bool ValidateType(AbstractComponent instance)
        {
            return instance is ThrusterEffect;
        }

        #endregion
    }
}
