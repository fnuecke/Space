﻿using System.Diagnostics;
using System.Globalization;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Engine.Serialization;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>
    /// Represents transformation of a 2d object (position/translation + rotation).
    /// </summary>
    public sealed class Transform : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

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
        public FarPosition Translation
#if DEBUG
        // Don't allow directly changing from outside.
        { get; private set; }
#else
            ;
#endif

        /// <summary>
        /// The angle of the current orientation.
        /// </summary>
        public float Rotation
#if DEBUG
        // Don't allow directly changing from outside.
        { get; private set; }
#else
        ;
#endif

        /// <summary>
        /// The translation to move to when performing the position update.
        /// </summary>
        private FarPosition _nextTranslation;

        /// <summary>
        /// Don't rely on float equality checks.
        /// </summary>
        private bool _translationChanged;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            // We do not want to trigger an update here, as it's the copy-
            // constructor, which must only be used when copying the whole
            // environment the component belongs to.
            var otherTransform = (Transform)other;
            Translation = otherTransform.Translation;
            _nextTranslation = Translation;
            _translationChanged = false;
            Rotation = otherTransform.Rotation;

            return this;
        }

        /// <summary>
        /// Initialize with the specified values.
        /// </summary>
        /// <param name="translation">The translation.</param>
        /// <param name="rotation">The rotation.</param>
        public Transform Initialize(FarPosition translation, float rotation)
        {
            SetTranslation(ref translation);
            SetRotation(MathHelper.WrapAngle(rotation));

            // Initialization must be called from a synchronous context (as
            // it must only be used when constructing the component). Thus
            // we want to trigger the update right now.
            ApplyTranslation();

            return this;
        }

        /// <summary>
        /// Initialize with the specified translation.
        /// </summary>
        /// <param name="translation">The translation.</param>
        public Transform Initialize(FarPosition translation)
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
            Initialize(FarPosition.Zero, rotation);

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            Translation = FarPosition.Zero;
            _nextTranslation = FarPosition.Zero;
            _translationChanged = false;
            Rotation = 0;
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
            _nextTranslation.X += x;
            _nextTranslation.Y += y;
            _translationChanged = true;
        }

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="value">The translation to add.</param>
        public void AddTranslation(ref FarPosition value)
        {
            _nextTranslation += value;
            _translationChanged = true;
        }

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="value">The translation to add.</param>
        public void AddTranslation(FarPosition value)
        {
            _nextTranslation += value;
            _translationChanged = true;
        }

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="value">The translation to add.</param>
        public void AddTranslation(ref Vector2 value)
        {
            _nextTranslation += value;
            _translationChanged = true;
        }

        /// <summary>
        /// Add the given translation to the current translation.
        /// </summary>
        /// <param name="value">The translation to add.</param>
        public void AddTranslation(Vector2 value)
        {
            _nextTranslation += value;
            _translationChanged = true;
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
        public void SetTranslation(FarValue x, FarValue y)
        {
            _nextTranslation.X = x;
            _nextTranslation.Y = y;
            _translationChanged = true;
        }

        /// <summary>
        /// Set the translation to the specified value.
        /// </summary>
        /// <param name="value">The new translation.</param>
        public void SetTranslation(ref FarPosition value)
        {
            _nextTranslation = value;
            _translationChanged = true;
        }

        /// <summary>
        /// Set the translation to the specified value.
        /// </summary>
        /// <param name="value">The new translation.</param>
        public void SetTranslation(FarPosition value)
        {
            _nextTranslation = value;
            _translationChanged = true;
        }

        /// <summary>
        /// Applies the translation set to be used next. Called from system,
        /// because we want to keep the setter in debug private to make sure
        /// no one actually writes directly to the translation variable.
        /// </summary>
        /// <remarks>
        /// This must be called from a synchronous context (i.e. not from a
        /// parallel system).
        /// </remarks>
        public void ApplyTranslation()
        {
            // Only update if we have to.
            if (!_translationChanged)
            {
                return;
            }

            TranslationChanged message;
            message.Entity = Entity;
            message.PreviousPosition = Translation;
            message.CurrentPosition = _nextTranslation;

            Translation = _nextTranslation;
            _translationChanged = false;

            Manager.SendMessage(message);
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
            Debug.Assert(!float.IsNaN(value));
            Rotation = MathHelper.WrapAngle(value);
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
            Debug.Assert(!_translationChanged);

            return base.Packetize(packet);
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            Debug.Assert(!_translationChanged);

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
            return base.ToString() + ", Translation=" + Translation + ", Rotation=" + Rotation.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
