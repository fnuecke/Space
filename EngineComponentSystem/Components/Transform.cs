using System;
using Engine.ComponentSystem.Components.Messages;
using Engine.Math;
using Engine.Serialization;

namespace Engine.ComponentSystem.Components
{
    /// <summary>
    /// Represents transformation of a 2d object (position/translation + rotation).
    /// </summary>
    public sealed class Transform : AbstractComponent
    {
        #region Properties
        
        /// <summary>
        /// Current position of the object.
        /// </summary>
        public FPoint Translation
        {
            get
            {
                return _translation;
            }
            set
            {
                if (value != _translation)
                {
                    var previousPosition = _translation;
                    _translation = value;
                    if (Entity != null)
                    {
                        Entity.SendMessage(TranslationChanged.Create(previousPosition));
                    }
                }
            }
        }

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        public Fixed Rotation { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Actual value holder for <c>Translation</c> property.
        /// </summary>
        private FPoint _translation;

        #endregion

        #region Modifiers

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="value">The translation to add.</param>
        public void AddTranslation(FPoint value)
        {
            Translation += value;
        }

        /// <summary>
        /// Add the given rotation to the current rotation
        /// 
        /// <para>
        /// This method will ensure that the value will remain in the
        /// interval of <c>(-PI, PI]</c>
        /// </para>
        /// </summary>
        /// <param name="value"></param>
        public void AddRotation(Fixed value)
        {
            Rotation += value;
            if (Rotation < -Fixed.PI)
            {
                Rotation += Fixed.PI * 2;
            }
            else if (Rotation > Fixed.PI)
            {
                Rotation -= Fixed.PI * 2;
            }
        }

        #endregion

        #region Serialization / Hashing

        public override Packet Packetize(Packet packet)
        {
            return base.Packetize(packet)
                .Write(Translation)
                .Write(Rotation);
        }

        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            Translation = packet.ReadFPoint();
            Rotation = packet.ReadFixed();
        }

        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(BitConverter.GetBytes(Translation.X.RawValue));
            hasher.Put(BitConverter.GetBytes(Translation.Y.RawValue));
            hasher.Put(BitConverter.GetBytes(Rotation.RawValue));
        }

        #endregion
    }
}
