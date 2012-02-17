using System;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// When attached to a component with a transform, this will automatically
    /// position the component to follow an ellipsoid path around a specified
    /// center entity.
    /// </summary>
    public sealed class EllipsePath : AbstractComponent
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
        internal float precomputedA;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float precomputedB;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float precomputedC;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float precomputedD;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float precomputedE;

        /// <summary>
        /// Precomputed for position calculation.
        /// </summary>
        /// <remarks>Do not change manually.</remarks>
        internal float precomputedF;

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

        #region Constructor

        public EllipsePath(int centerEntityId, float majorRadius, float minorRadius, float angle, float period, float periodOffset)
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
        }

        public EllipsePath()
        {
        }

        #endregion

        #region Precomputation

        /// <summary>
        /// Fills in precomputable values.
        /// </summary>
        private void Precompute()
        {
            // If our angle changed, recompute our sine and cosine.
            var SinPhi = (float)System.Math.Sin(_angle);
            var CosPhi = (float)System.Math.Cos(_angle);
            var F = (float)System.Math.Sqrt(System.Math.Abs(
                _minorRadius * _minorRadius - _majorRadius * _majorRadius));
            
            precomputedA = F * CosPhi;
            precomputedB = MajorRadius * CosPhi;
            precomputedC = MinorRadius * SinPhi;
            precomputedD = F * SinPhi;
            precomputedE = MajorRadius * SinPhi;
            precomputedF = MinorRadius * CosPhi;
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

            hasher.Put(BitConverter.GetBytes(_angle));
            hasher.Put(BitConverter.GetBytes(_majorRadius));
            hasher.Put(BitConverter.GetBytes(_minorRadius));
            hasher.Put(BitConverter.GetBytes(CenterEntityId));
            hasher.Put(BitConverter.GetBytes(Period));
            hasher.Put(BitConverter.GetBytes(PeriodOffset));
        }

        #endregion

        #region Copying

        /// <summary>
        /// Creates a deep copy of this instance by reusing the specified
        /// instance, if possible.
        /// </summary>
        /// <param name="into"></param>
        /// <returns>
        /// An independent (deep) clone of this instance.
        /// </returns>
        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (EllipsePath)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Angle = _angle;
                copy.MajorRadius = _majorRadius;
                copy.MinorRadius = _minorRadius;
                copy.CenterEntityId = CenterEntityId;
                copy.Period = Period;
                copy.PeriodOffset = PeriodOffset;
            }

            return copy;
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
            return base.ToString() + ", CenterEntityId = " + CenterEntityId.ToString() + ", MajorRadius = " + MajorRadius.ToString() + ", MinorRadius = " + MinorRadius.ToString() + ", Angle = " + Angle.ToString() + ", Period = " + Period.ToString() + ", PeriodOffset = " + PeriodOffset.ToString();
        }

        #endregion
    }
}
