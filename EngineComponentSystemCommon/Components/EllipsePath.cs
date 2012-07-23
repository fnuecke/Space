using System;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// When attached to a component with a transform, this will automatically
    /// position the component to follow an ellipsoid path around a specified
    /// center entity.
    /// </summary>
    public sealed class EllipsePath : Component
    {
        #region Properties

        /// <summary>
        /// The angle of the ellipse's major axis to the global x axis.
        /// </summary>
        public float Angle
        {
            get
            {
                return _angle;
            }
            set
            {
                if (value != _angle)
                {
                    _angle = value;
                    Precompute();
                }
            }
        }

        /// <summary>
        /// The radius of the ellipse along the major axis.
        /// </summary>
        public float MajorRadius
        {
            get
            {
                return _majorRadius;
            }
            set
            {
                if (value != _majorRadius)
                {
                    _majorRadius = value;
                    Precompute();
                }
            }
        }

        /// <summary>
        /// The radius of the ellipse along the minor axis.
        /// </summary>
        public float MinorRadius
        {
            get
            {
                return _minorRadius;
            }
            set
            {
                if (value != _minorRadius)
                {
                    _minorRadius = value;
                    Precompute();
                }
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The id of the entity the entity this component belongs to
        /// rotates around.
        /// </summary>
        public int CenterEntityId;

        /// <summary>
        /// The time in frames it takes for the component to perform a full
        /// rotation around its center.
        /// </summary>
        public float Period;

        /// <summary>
        /// Starting offset of our period (otherwise all objects with the same
        /// period will always be at the same angle...)
        /// </summary>
        public float PeriodOffset;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float PrecomputedA;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float PrecomputedB;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float PrecomputedC;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float PrecomputedD;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float PrecomputedE;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float PrecomputedF;

        /// <summary>
        /// Actual value of the angle.
        /// </summary>
        private float _angle;

        /// <summary>
        /// Actual value of major radius.
        /// </summary>
        private float _majorRadius;

        /// <summary>
        /// Actual value of minor radius.
        /// </summary>
        private float _minorRadius;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherEllipsePath = (EllipsePath)other;
            Angle = otherEllipsePath._angle;
            MajorRadius = otherEllipsePath._majorRadius;
            MinorRadius = otherEllipsePath._minorRadius;
            CenterEntityId = otherEllipsePath.CenterEntityId;
            Period = otherEllipsePath.Period;
            PeriodOffset = otherEllipsePath.PeriodOffset;

            return this;
        }

        /// <summary>
        /// Initialize the component with the specified values.
        /// </summary>
        /// <param name="centerEntityId">The center entity's id.</param>
        /// <param name="majorRadius">The major radius.</param>
        /// <param name="minorRadius">The minor radius.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="period">The period.</param>
        /// <param name="periodOffset">The period offset.</param>
        public EllipsePath Initialize(int centerEntityId, float majorRadius, float minorRadius,
            float angle, float period, float periodOffset)
        {
            this.CenterEntityId = centerEntityId;
            if (majorRadius < minorRadius)
            {
                this.MajorRadius = minorRadius;
                this.MinorRadius = majorRadius;
            }
            else
            {
                this.MajorRadius = majorRadius;
                this.MinorRadius = minorRadius;
            }
            this.Angle = angle;
            this.Period = period;
            this.PeriodOffset = periodOffset;

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Angle = 0;
            MajorRadius = 0;
            MinorRadius = 0;
            CenterEntityId = 0;
            Period = 0;
            PeriodOffset = 0;
        }

        #endregion

        #region Precomputation

        /// <summary>
        /// Fills in precomputable values.
        /// </summary>
        private void Precompute()
        {
            // If our angle changed, recompute our sine and cosine.
            var sinPhi = (float)Math.Sin(_angle);
            var cosPhi = (float)Math.Cos(_angle);
            var f = (float)Math.Sqrt(Math.Abs(
                _minorRadius * _minorRadius - _majorRadius * _majorRadius));
            
            PrecomputedA = f * cosPhi;
            PrecomputedB = MajorRadius * cosPhi;
            PrecomputedC = MinorRadius * sinPhi;
            PrecomputedD = f * sinPhi;
            PrecomputedE = MajorRadius * sinPhi;
            PrecomputedF = MinorRadius * cosPhi;
        }

        #endregion

        #region Serialization / Hashing

        /// <summary>
        /// Write the object's state to the given packet.
        /// </summary>
        /// <param name="packet">The packet to write the data to.</param>
        /// <returns>
        /// The packet after writing.
        /// </returns>
        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(_angle)
                .Write(_majorRadius)
                .Write(_minorRadius)
                .Write(CenterEntityId)
                .Write(Period)
                .Write(PeriodOffset);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);

            Angle = packet.ReadSingle();
            MajorRadius = packet.ReadSingle();
            MinorRadius = packet.ReadSingle();
            CenterEntityId = packet.ReadInt32();
            Period = packet.ReadSingle();
            PeriodOffset = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher.Put(_angle);
            hasher.Put(_majorRadius);
            hasher.Put(_minorRadius);
            hasher.Put(CenterEntityId);
            hasher.Put(Period);
            hasher.Put(PeriodOffset);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", CenterEntityId = " + CenterEntityId + ", MajorRadius = " + MajorRadius + ", MinorRadius = " + MinorRadius + ", Angle = " + Angle + ", Period = " + Period + ", PeriodOffset = " + PeriodOffset;
        }

        #endregion
    }
}
