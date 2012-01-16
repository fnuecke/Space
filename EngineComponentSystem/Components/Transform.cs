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
        /// <remarks>
        /// Do <em>not</em> set this field directly, use the modifier methods
        /// instead, as these will trigger the necessary messages.
        /// </remarks>
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
#if DEBUG
            if (float.IsNaN(value.X) || float.IsNaN(value.Y))
            {
                throw new ArgumentException("value");
            }
#endif

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

        /// <summary>
        /// Set the translation to the specified values.
        /// </summary>
        /// <param name="x">The new x coordinate.</param>
        /// <param name="y">The new y coordinate.</param>
        public void SetTranslation(float x, float y)
        {
#if DEBUG
            if (float.IsNaN(x) || float.IsNaN(y))
            {
                throw new ArgumentException(float.IsNaN(x) ? "x" : "y");
            }
#endif

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

        /// <summary>
        /// Set the translation to the specified value.
        /// </summary>
        /// <param name="value">The new translation.</param>
        public void SetTranslation(ref Vector2 value)
        {
            SetTranslation(value.X, value.Y);
        }

        /// <summary>
        /// Set the translation to the specified value.
        /// </summary>
        /// <param name="value">The new translation.</param>
        public void SetTranslation(Vector2 value)
        {
            SetTranslation(ref value);
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
#if DEBUG
            if (float.IsNaN(value))
            {
                throw new ArgumentException("value");
            }
#endif

            SetRotation(Rotation + value);
        }

        public void SetRotation(float value)
        {
#if DEBUG
            if (float.IsNaN(value))
            {
                throw new ArgumentException("value");
            }
#endif

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
                .Write(Translation)
                .Write(Rotation);
        }

        /// <summary>
        /// Bring the object to the state in the given packet.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(Packet packet)
        {
            base.Depacketize(packet);
            
            Translation = packet.ReadVector2();
            Rotation = packet.ReadSingle();
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(BitConverter.GetBytes(Translation.X));
            hasher.Put(BitConverter.GetBytes(Translation.Y));
            hasher.Put(BitConverter.GetBytes(Rotation));
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

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Translation = " + Translation.ToString() + ", Rotation = " + Rotation.ToString();
        }

        #endregion
    }
}
