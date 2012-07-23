using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Components;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// Represents transformation of a 2d object (position/translation + rotation).
    /// </summary>
    public sealed class Transform : Component
    {
        #region Properties

        /// <summary>
        /// Current position of the object.
        /// </summary>
        /// <remarks>
        /// This is not ideal performance wise, as we cannot pass this value
        /// per reference directly, but it's worth it regarding the security
        /// it brings regarding that it cannot be set directly, as we must
        /// make sure the <c>TranslationChanged</c> message is sent whenever
        /// this value changes.
        /// </remarks>
        public Vector2 Translation
#if DEBUG // Don't allow directly changing from outside.
        {
            get { return _translation; }
        }
        private Vector2 _translation;
#else
            ;
#endif

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        public float Rotation
#if DEBUG // Don't allow directly changing from outside.
        {
            get { return _rotation; }
        }
        private float _rotation;
#else
        ;
#endif

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherTransform = (Transform)other;
            SetTranslation(otherTransform.Translation);
            SetRotation(otherTransform.Rotation);

            return this;
        }

        /// <summary>
        /// Initialize with the specified values.
        /// </summary>
        /// <param name="translation">The translation.</param>
        /// <param name="rotation">The rotation.</param>
        public Transform Initialize(Vector2 translation, float rotation)
        {
            // Don't use setters because we don't want to trigger messages in
            // initialization.
            SetTranslation(ref translation);
            SetRotation(MathHelper.WrapAngle(rotation));

            return this;
        }

        /// <summary>
        /// Initialize with the specified translation.
        /// </summary>
        /// <param name="translation">The translation.</param>
        public Transform Initialize(Vector2 translation)
        {
            Initialize(translation, 0);

            return this;
        }

        /// <summary>
        /// Initialize with the specified rotation.
        /// </summary>
        /// <param name="rotation">The rotation.</param>
        public Transform Initialize(float rotation)
        {
            Initialize(Vector2.Zero, rotation);

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            SetTranslation(Vector2.Zero);
            SetRotation(0);
        }

        #endregion

        #region Modifiers

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="x">The new x translation to add.</param>
        /// <param name="y">The new y translation to add.</param>
        public void AddTranslation(float x, float y)
        {
            SetTranslation(Translation.X + x, Translation.Y + y);
        }

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="value">The translation to add.</param>
        public void AddTranslation(ref Vector2 value)
        {
            SetTranslation(Translation.X + value.X, Translation.Y + value.Y);
        }

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="value">The translation to add.</param>
        public void AddTranslation(Vector2 value)
        {
            AddTranslation(ref value);
        }

        /// <summary>
        /// Set the translation to the specified values.
        /// </summary>
        /// <remarks>
        /// This is not part of the property, to make sure at least the getter
        /// of it gets inlined properly (or at least increase chances it is).
        /// </remarks>
        /// <param name="x">The new x coordinate.</param>
        /// <param name="y">The new y coordinate.</param>
        public void SetTranslation(float x, float y)
        {
            System.Diagnostics.Debug.Assert(!float.IsNaN(x));
            System.Diagnostics.Debug.Assert(!float.IsNaN(y));

            if (Translation.X != x || Translation.Y != y)
            {
                TranslationChanged message;
                message.Entity = Entity;
                message.PreviousPosition = Translation;

#if DEBUG
                _translation.X = x;
                _translation.Y = y;
#else
                Translation.X = x;
                Translation.Y = y;
#endif

                message.CurrentPosition = Translation;

                if (Manager != null)
                {
                    Manager.SendMessage(ref message);
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
            SetRotation(Rotation + value);
        }

        /// <summary>
        /// Sets the rotation to the specified value. Clamps the rotation to be
        /// in the interval of <c>(-PI, PI]</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void SetRotation(float value)
        {
            System.Diagnostics.Debug.Assert(!float.IsNaN(value));

#if DEBUG
            _rotation = MathHelper.WrapAngle(value);
#else
            Rotation = MathHelper.WrapAngle(value);
#endif
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

            SetTranslation(packet.ReadVector2());
            SetRotation(packet.ReadSingle());
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Util.Hasher hasher)
        {
            base.Hash(hasher);
            
            hasher.Put(Translation);
            hasher.Put(Rotation);
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
            return base.ToString() + ", Translation = " + Translation + ", Rotation = " + Rotation;
        }

        #endregion
    }
}
