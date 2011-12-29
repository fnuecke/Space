using System;
using Engine.ComponentSystem.Parameterizations;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents rotation speed of an object.
    /// 
    /// <para>
    /// Requires: <c>Transform</c>.
    /// </para>
    /// </summary>
    public sealed class Spin : AbstractComponent
    {
        #region Properties

        /// <summary>
        /// The current rotation speed of the object.
        /// </summary>
        public float Value { get; set; }

        #endregion

        #region Constructor

        public Spin(float spin)
        {
            this.Value = spin;
        }

        public Spin()
            : this(0)
        {
        }

        #endregion

        #region Logic

        /// <summary>
        /// Updates the rotation based on this spin.
        /// </summary>
        /// <param name="parameterization">The parameterization to use.</param>
        public override void Update(object parameterization)
        {
#if DEBUG
            base.Update(parameterization);
#endif
            var transform = Entity.GetComponent<Transform>();

            // Apply rotation if transform is available.
            if (transform != null)
            {
                transform.AddRotation(Value);
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
                .Write(Value);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            Value = packet.ReadSingle();
        }

        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(BitConverter.GetBytes(Value));
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + Value.ToString();
        }

        #endregion
    }
}
