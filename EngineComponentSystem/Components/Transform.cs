using System;
using Engine.ComponentSystem.Components.Messages;
using Engine.Serialization;
using Microsoft.Xna.Framework;

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
        public Vector2 Translation
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
        public float Rotation { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Actual value holder for <c>Translation</c> property.
        /// </summary>
        private Vector2 _translation;

        #endregion

        #region Modifiers

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="value">The translation to add.</param>
        public void AddTranslation(Vector2 value)
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
        public void AddRotation(float value)
        {
            Rotation += value;
            if (Rotation < -System.Math.PI)
            {
                Rotation += (float)System.Math.PI * 2;
            }
            else if (Rotation > System.Math.PI)
            {
                Rotation -= (float)System.Math.PI * 2;
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
            
            Translation = packet.ReadVector2();
            Rotation = packet.ReadSingle();
        }

        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(BitConverter.GetBytes(Translation.X));
            hasher.Put(BitConverter.GetBytes(Translation.Y));
            hasher.Put(BitConverter.GetBytes(Rotation));
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return GetType().Name + ": " + Translation.ToString() + ", " + Rotation.ToString();
        }

        #endregion
    }
}
