using Engine.Physics.Systems;
using Microsoft.Xna.Framework;

namespace EnginePhysicsTests
{
    sealed class DebugPhysicsRenderSystem : AbstractDebugPhysicsRenderSystem
    {
        /// <summary>
        /// Gets or sets the scale at which to render the debug view.
        /// </summary>
        public float Scale
        {
            get { return _scale; }
            set { _scale = MathHelper.Clamp(value, 0.01f, 2); }
        }

        /// <summary>
        /// Gets or sets the offset to render at, in simulation space.
        /// </summary>
        public Vector2 Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        private float _scale = 1;

        private Vector2 _offset = Vector2.Zero;

        /// <summary>
        /// Gets the display scaling factor (camera zoom).
        /// </summary>
        protected override float GetScale()
        {
            return Scale;
        }
        
        /// <summary>
        /// Gets the display transform (camera position/rotation).
        /// </summary>
        protected override Matrix GetTransform()
        {
            return Matrix.CreateTranslation(Offset.X, Offset.Y, 0);
        }
    }
}
