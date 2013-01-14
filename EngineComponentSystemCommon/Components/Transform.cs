using System.Diagnostics;
using Engine.ComponentSystem.Common.Messages;
using Engine.ComponentSystem.Components;
using Engine.FarMath;
using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Common.Components
{
    /// <summary>Represents transformation of a 2d object (position/translation + rotation).</summary>
    public sealed class Transform : Component, IIndexable
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>The index group mask determining which indexes the component will be tracked by.</summary>
        public ulong IndexGroupsMask
        {
            get { return _indexGroupsMask; }
            set
            {
                if (value == _indexGroupsMask)
                {
                    return;
                }
                
                var oldMask = _indexGroupsMask;
                _indexGroupsMask = value;

                if (Manager == null)
                {
                    return;
                }

                IndexGroupsChanged message;
                message.Component = this;
                message.AddedIndexGroups = value & ~oldMask;
                message.RemovedIndexGroups = oldMask & ~value;
                Manager.SendMessage(message);
            }
        }

        /// <summary>
        ///     Gets or sets the size of this component's bounds. This will be offset by the component's translation to get
        ///     the world bounds.
        /// </summary>
        public FarRectangle Bounds
        {
            get { return _bounds; }
            set
            {
                if (_bounds.Equals(value))
                {
                    return;
                }

                _bounds = value;

                if (Manager == null)
                {
                    return;
                }

                IndexBoundsChanged message;
                message.Component = this;
                message.Bounds = Bounds;
                Manager.SendMessage(message);
            }
        }

        /// <summary>Current position of the object.</summary>
        /// <remarks>
        ///     This is not ideal performance wise, as we cannot pass this value per reference directly, but it's worth it
        ///     regarding the security it brings regarding that it cannot be set directly, as we must make sure the
        ///     <c>TranslationChanged</c> message is sent whenever this value changes.
        /// </remarks>
        public FarPosition Translation
        {
            get { return _translation; }
            set
            {
                _nextTranslation = value;
                _translationChanged = true;
            }
        }

        /// <summary>The angle of the current orientation.</summary>
        public float Rotation
        {
            get { return _rotation; }
            set
            {
                Debug.Assert(!float.IsNaN(value));
                _nextRotation = value;
                _rotationChanged = true;
            }
        }

        #endregion

        #region Fields

        /// <summary>Index group bitmask for the component.</summary>
        private ulong _indexGroupsMask;

        /// <summary>Local bounds of the component.</summary>
        private FarRectangle _bounds;

        /// <summary>The current translation of the component.</summary>
        private FarPosition _translation;

        /// <summary>The translation to move to when performing the next update.</summary>
        private FarPosition _nextTranslation;

        /// <summary>Don't rely on float equality checks.</summary>
        private bool _translationChanged;

        /// <summary>The current rotation of the component.</summary>
        private float _rotation;

        /// <summary>The rotation to set to when performing the next update.</summary>
        private float _nextRotation;

        /// <summary>Don't rely on float equality checks.</summary>
        private bool _rotationChanged;

        #endregion

        #region Initialization

        /// <summary>Initialize the component by using another instance of its type.</summary>
        /// <param name="other">The component to copy the values from.</param>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            // We do not want to trigger an update here, as it's the copy-
            // constructor, which must only be used when copying the whole
            // environment the component belongs to.
            var otherTransform = (Transform) other;
            _indexGroupsMask = otherTransform._indexGroupsMask;
            _bounds = otherTransform._bounds;
            _translation = otherTransform._translation;
            _nextTranslation = otherTransform._nextTranslation;
            _translationChanged = otherTransform._translationChanged;
            _rotation = otherTransform._rotation;
            _nextRotation = otherTransform._nextRotation;
            _rotationChanged = otherTransform._rotationChanged;

            return this;
        }

        /// <summary>Initialize with the specified values.</summary>
        /// <param name="bounds">The bounds of the component.</param>
        /// <param name="translation">The initial translation.</param>
        /// <param name="rotation">The initial rotation.</param>
        /// <param name="indexGroupsMask">The index groups mask.</param>
        /// <returns>This component.</returns>
        public Transform Initialize(FarRectangle bounds, FarPosition translation, float rotation = 0, ulong indexGroupsMask = 0)
        {
            Bounds = bounds;
            IndexGroupsMask = indexGroupsMask;

            Translation = translation;
            Rotation = rotation;

            // Initialization must be called from a synchronous context (as
            // it must only be used when constructing the component). Thus
            // we want to trigger the update right now.
            Update();

            return this;
        }

        /// <summary>Initialize with the specified rotation.</summary>
        /// <param name="translation">The initial translation.</param>
        /// <param name="rotation">The initial rotation.</param>
        /// <param name="indexGroupsMask">The index groups mask.</param>
        /// <returns></returns>
        public Transform Initialize(FarPosition translation, float rotation = 0, ulong indexGroupsMask = 0)
        {
            return Initialize(FarRectangle.Empty, translation, rotation, indexGroupsMask);
        }
        
        /// <summary>Initialize with the specified rotation.</summary>
        /// <param name="rotation">The initial rotation.</param>
        /// <param name="indexGroupsMask">The index groups mask.</param>
        /// <returns></returns>
        public Transform Initialize(float rotation = 0, ulong indexGroupsMask = 0)
        {
            return Initialize(FarRectangle.Empty, FarPosition.Zero, rotation, indexGroupsMask);
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            _indexGroupsMask = 0;
            _bounds = FarRectangle.Empty;
            _translation = FarPosition.Zero;
            _nextTranslation = FarPosition.Zero;
            _translationChanged = false;
            _rotation = 0;
            _nextRotation = 0;
            _rotationChanged = false;
        }

        #endregion

        #region Modifiers
        
        /// <summary>Computes the current world bounds of the component, to allow adding it to indexes.</summary>
        /// <returns>The current world bounds of the component.</returns>
        public FarRectangle ComputeWorldBounds()
        {
            var worldBounds = _bounds;
            worldBounds.Offset(_translation);
            return worldBounds;
        }

        /// <summary>
        ///     Applies the translation set to be used next. Called from system, because we want to keep the setter in debug
        ///     private to make sure no one actually writes directly to the translation variable.
        /// </summary>
        /// <remarks>This must be called from a synchronous context (i.e. not from a parallel system).</remarks>
        public void Update()
        {
            if (_translationChanged)
            {
                TranslationChanged message;
                message.Component = this;
                message.PreviousPosition = _translation;
                message.CurrentPosition = _nextTranslation;

                _translation = _nextTranslation;
                _translationChanged = false;

                Manager.SendMessage(message);
            }

            if (_rotationChanged)
            {
                RotationChanged message;
                message.Component = this;
                message.PreviousRotation = _rotation;
                message.CurrentRotation = _nextRotation;

                _rotation = MathHelper.WrapAngle(_nextRotation);
                _rotationChanged = false;

                Manager.SendMessage(message);
            }
        }

        #endregion
    }
}