using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
#endif

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>
    ///     When attached to a component with a transform, this will automatically position the component to follow an
    ///     ellipsoid path around a specified center entity.
    /// </summary>
    public sealed class EllipsePath : Component
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion
        
        #region Constants

        /// <summary>
        /// Get the interface's type id once, for performance.
        /// </summary>
        private static readonly int TransformTypeId = ComponentSystem.Manager.GetComponentTypeId<ITransform>();

        #endregion

        #region Properties

        /// <summary>The angle of the ellipse's major axis to the global x axis.</summary>
        [PublicAPI]
        public float Angle
        {
            get { return _angle; }
            set
            {
                _angle = value;
                Precompute();
            }
        }

        /// <summary>The radius of the ellipse along the major axis.</summary>
        [PublicAPI]
        public float MajorRadius
        {
            get { return _majorRadius; }
            set
            {
                _majorRadius = value;
                Precompute();
            }
        }

        /// <summary>The radius of the ellipse along the minor axis.</summary>
        [PublicAPI]
        public float MinorRadius
        {
            get { return _minorRadius; }
            set
            {
                _minorRadius = value;
                Precompute();
            }
        }

        #endregion

        #region Fields

        /// <summary>The id of the entity the entity this component belongs to rotates around.</summary>
        [PublicAPI]
        public int CenterEntityId;

        /// <summary>The time in frames it takes for the component to perform a full rotation around its center.</summary>
        [PublicAPI]
        public float Period;

        /// <summary>Starting offset of our period (otherwise all objects with the same period will always be at the same angle...)</summary>
        [PublicAPI]
        public float PeriodOffset;

        /// <summary>Precomputed for position calculation.</summary>
        /// <remarks>Do not change manually.</remarks>
        [PacketizeIgnore]
        private float _precomputedA;

        /// <summary>Precomputed for position calculation.</summary>
        /// <remarks>Do not change manually.</remarks>
        [PacketizeIgnore]
        private float _precomputedB;

        /// <summary>Precomputed for position calculation.</summary>
        /// <remarks>Do not change manually.</remarks>
        [PacketizeIgnore]
        private float _precomputedC;

        /// <summary>Precomputed for position calculation.</summary>
        /// <remarks>Do not change manually.</remarks>
        [PacketizeIgnore]
        private float _precomputedD;

        /// <summary>Precomputed for position calculation.</summary>
        /// <remarks>Do not change manually.</remarks>
        [PacketizeIgnore]
        private float _precomputedE;

        /// <summary>Precomputed for position calculation.</summary>
        /// <remarks>Do not change manually.</remarks>
        [PacketizeIgnore]
        private float _precomputedF;

        /// <summary>Actual value of the angle.</summary>
        private float _angle;

        /// <summary>Actual value of major radius.</summary>
        private float _majorRadius;

        /// <summary>Actual value of minor radius.</summary>
        private float _minorRadius;

        #endregion

        #region Initialization

        /// <summary>Initialize the component with the specified values.</summary>
        /// <param name="centerEntityId">The center entity's id.</param>
        /// <param name="majorRadius">The major radius.</param>
        /// <param name="minorRadius">The minor radius.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="period">The period.</param>
        /// <param name="periodOffset">The period offset.</param>
        public EllipsePath Initialize(
            int centerEntityId,
            float majorRadius,
            float minorRadius,
            float angle,
            float period,
            float periodOffset)
        {
            if (period == 0.0)
            {
                throw new ArgumentException("Period must not be zero.", "period");
            }
            CenterEntityId = centerEntityId;
            if (majorRadius < minorRadius)
            {
                MajorRadius = minorRadius;
                MinorRadius = majorRadius;
            }
            else
            {
                MajorRadius = majorRadius;
                MinorRadius = minorRadius;
            }
            Angle = angle;
            Period = period;
            PeriodOffset = periodOffset;

            // Move to somewhere on the path. This will still lead to a jump on the first
            // update, but at least it's less likely someone sees it.
            Update(0);

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            Angle = 0;
            MajorRadius = 0;
            MinorRadius = 0;
            CenterEntityId = 0;
            Period = MathHelper.TwoPi;
            PeriodOffset = 0;
        }

        #endregion

        #region Logic

        public void Update(long frame)
        {
            // Get the center, the position of the entity we're rotating around.
            var center = ((ITransform) Manager.GetComponent(CenterEntityId, TransformTypeId)).Position;

            // Get the angle based on the time passed.
            var t = PeriodOffset + MathHelper.Pi * frame / Period;
            var sinT = (float) System.Math.Sin(t);
            var cosT = (float) System.Math.Cos(t);

            // Compute the current position and set it.
            ((ITransform) Manager.GetComponent(Entity, TransformTypeId)).Position = new WorldPoint(
                center.X + _precomputedA + _precomputedB * cosT - _precomputedC * sinT,
                center.Y + _precomputedD + _precomputedE * cosT + _precomputedF * sinT);
        }

        /// <summary>Fills in precomputable values.</summary>
        private void Precompute()
        {
            // If our angle changed, recompute our sine and cosine.
            var sinPhi = (float) System.Math.Sin(_angle);
            var cosPhi = (float) System.Math.Cos(_angle);
            var f = (float) System.Math.Sqrt(System.Math.Abs(_minorRadius * _minorRadius - _majorRadius * _majorRadius));

            _precomputedA = f * cosPhi;
            _precomputedB = MajorRadius * cosPhi;
            _precomputedC = MinorRadius * sinPhi;
            _precomputedD = f * sinPhi;
            _precomputedE = MajorRadius * sinPhi;
            _precomputedF = MinorRadius * cosPhi;
        }

        #endregion

        #region Serialization

        [OnPostDepacketize]
        public void Depacketize(IReadablePacket packet)
        {
            Precompute();
        }

        #endregion
    }
}