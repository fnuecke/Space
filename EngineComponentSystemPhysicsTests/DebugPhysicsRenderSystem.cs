using Engine.ComponentSystem.Physics.Systems;
using Engine.Serialization;
using Microsoft.Xna.Framework;

#if FARMATH
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Engine.FarMath.FarPosition;
using WorldTransform = Engine.FarMath.FarTransform;
#else
using LocalPoint = Microsoft.Xna.Framework.Vector2;
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldTransform = Microsoft.Xna.Framework.Matrix;
#endif

namespace Engine.ComponentSystem.Physics.Tests
{
    /// <summary>This implementation plays camera for the test framework.</summary>
    internal sealed class DebugPhysicsRenderSystem : AbstractDebugPhysicsRenderSystem
    {
        #region Properties

        /// <summary>Gets or sets the scale at which to render the debug view.</summary>
        public float Scale
        {
            get { return _scale; }
            set { _scale = MathHelper.Clamp(value, 0.01f, 2); }
        }

        /// <summary>Gets or sets the offset to render at, in simulation space.</summary>
        public WorldPoint Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        #endregion

        #region Fields

        /// <summary>Backing scale field for initial value.</summary>
        [PacketizeIgnore]
        private float _scale = 1;

        /// <summary>Backing offset field for transformation on setting (to avoid recomputation each render pass).</summary>
        [PacketizeIgnore]
        private WorldPoint _offset = WorldPoint.Zero;

        #endregion

        #region Implementation

        /// <summary>Gets the display scaling factor (camera zoom).</summary>
        protected override float GetScale()
        {
            return Scale;
        }

        /// <summary>Gets the display transform (camera position/rotation).</summary>
        protected override WorldTransform GetTransform()
        {
#if FARMATH
            WorldTransform transform;
            transform.Translation = _offset;
            transform.Matrix = Matrix.Identity;
            return transform;
#else
            return Matrix.CreateTranslation(_offset.X, _offset.Y, 0);
#endif
        }

        #endregion
    }
}