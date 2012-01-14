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
        #region Fields

        /// <summary>
        /// Current position of the object.
        /// </summary>
        public Vector2 Translation;

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        public float Rotation;

        #endregion

        #region Constructor

        public Transform(Vector2 translation, float rotation)
        {
            this.Translation = translation;
            this.Rotation = rotation;
        }

        public Transform(Vector2 translation)
            : this(translation, 0)
        {
        }

        public Transform(float rotation)
            : this(Vector2.Zero, rotation)
        {
        }

        public Transform()
            : this(Vector2.Zero, 0)
        {
        }

        #endregion

        #region Modifiers

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="value">The translation to add.</param>
        public void AddTranslation(ref Vector2 value)
        {
            if (value.X != 0 || value.Y != 0)
            {
                TranslationChanged message;
                message.PreviousPosition = Translation;

                Translation.X += value.X;
                Translation.Y += value.Y;

                if (Entity != null)
                {
                    Entity.SendMessage(ref message);
                }
            }
        }

        public void SetTranslation(float x, float y)
        {
            if (Translation.X != x && Translation.Y != y)
            {
                TranslationChanged message;
                message.PreviousPosition = Translation;

                Translation.X = x;
                Translation.Y = y;

                if (Entity != null)
                {
                    Entity.SendMessage(ref message);
                }
            }
        }

        public void SetTranslation(ref Vector2 value)
        {
            SetTranslation(value.X, value.Y);
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
            SetRotation(Rotation + value);
        }

        public void SetRotation(float value)
        {
            if (value < -System.Math.PI)
            {
                value += (float)System.Math.PI * 2;
            }
            else if (value > System.Math.PI)
            {
                value -= (float)System.Math.PI * 2;
            }
            Rotation = value;
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

        #region Copying

        public override AbstractComponent DeepCopy(AbstractComponent into)
        {
            var copy = (Transform)base.DeepCopy(into);

            if (copy == into)
            {
                copy.Translation = Translation;
                copy.Rotation = Rotation;
            }

            return copy;
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
