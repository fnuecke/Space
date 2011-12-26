using System;
using Engine.ComponentSystem.Entities;
using Engine.ComponentSystem.Parameterizations;
using Engine.Math;
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
        /// The id of the entity the entity this component belongs to
        /// rotates around.
        /// </summary>
        public int CenterEntityId { get; set; }

        /// <summary>
        /// The radius of the ellipse along the major axis.
        /// </summary>
        public Fixed MajorRadius { get; set; }

        /// <summary>
        /// The radius of the ellipse along the minor axis.
        /// </summary>
        public Fixed MinorRadius { get; set; }

        /// <summary>
        /// The angle of the ellipse's major axis to the global x axis.
        /// </summary>
        public Fixed Angle
        {
            get
            {
                return _angle;
            }
            set
            {
                _angle = value;
                _sinPhi = Fixed.Sin(value);
                _cosPhi = Fixed.Cos(value);
            }
        }

        /// <summary>
        /// The time in frames it takes for the component to perform a full
        /// rotation around its center.
        /// </summary>
        public int Period { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Actual value of the angle.
        /// </summary>
        Fixed _angle;

        /// <summary>
        /// Precomputed sine of the angle.
        /// </summary>
        Fixed _sinPhi;

        /// <summary>
        /// Precomputed cosine of the angle.
        /// </summary>
        Fixed _cosPhi = (Fixed)1;

        #endregion

        #region Logic

        /// <summary>
        /// Updates an objects position based on this velocity.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            var transform = Entity.GetComponent<Transform>();

            // Only if a transform is set.
            if (transform != null)
            {
                var args = (DefaultLogicParameterization)parameterization;

                // Try to get the center of the entity we're rotating around.
                FPoint center = FPoint.Zero;
                var centerEntity = Entity.Manager.GetEntity(CenterEntityId);
                if (centerEntity != null)
                {
                    var centerTransform = centerEntity.GetComponent<Transform>();
                    if (centerTransform != null)
                    {
                        center = centerTransform.Translation;
                    }
                }

                // Get the angle based on the time passed.
                var t = Fixed.PI * (Fixed)args.Frame / (Fixed)Period;
                var sinT = Fixed.Sin(t);
                var cosT = Fixed.Cos(t);

                var f = Fixed.Sqrt(Fixed.Abs(MinorRadius * MinorRadius - MajorRadius * MajorRadius));

                // Compute the current position and set it.
                transform.Translation = FPoint.Create(
                    center.X + f * _cosPhi + MajorRadius * cosT * _cosPhi - MinorRadius * sinT * _sinPhi,
                    center.Y + f * _sinPhi + MajorRadius * cosT * _sinPhi + MinorRadius * sinT * _cosPhi
                );
            }
        }

        /// <summary>
        /// Accepts <c>DefaultLogicParameterization</c>s.
        /// </summary>
        /// <param name="parameterizationType">the type to check.</param>
        /// <returns>whether the type's supported or not.</returns>
        public override bool SupportsParameterization(Type parameterizationType)
        {
            return parameterizationType == typeof(DefaultLogicParameterization);
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(CenterEntityId)
                .Write(MajorRadius)
                .Write(MinorRadius)
                .Write(Angle)
                .Write(Period);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            CenterEntityId = packet.ReadInt32();
            MajorRadius = packet.ReadFixed();
            MinorRadius = packet.ReadFixed();
            Angle = packet.ReadFixed();
            Period = packet.ReadInt32();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
            hasher.Put(BitConverter.GetBytes(CenterEntityId));
            hasher.Put(BitConverter.GetBytes(MajorRadius.RawValue));
            hasher.Put(BitConverter.GetBytes(MinorRadius.RawValue));
            hasher.Put(BitConverter.GetBytes(Angle.RawValue));
            hasher.Put(BitConverter.GetBytes(Period));
        }

        #endregion
    }
}
