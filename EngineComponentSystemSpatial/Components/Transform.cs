using System.Diagnostics;
using Engine.ComponentSystem.Spatial.Messages;
using Engine.ComponentSystem.Components;
using Microsoft.Xna.Framework;

#if FARMATH
using WorldPoint = Engine.FarMath.FarPosition;
using WorldBounds = Engine.FarMath.FarRectangle;
#else
using WorldPoint = Microsoft.Xna.Framework.Vector2;
using WorldBounds = Engine.Math.RectangleF;
#endif

namespace Engine.ComponentSystem.Spatial.Components
{
    /// <summary>Represents transformation of a 2d object (position/translation + angle/orientation).</summary>
    public sealed class Transform : Component, ITransform
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

        /// <summary>Current position of the object.</summary>
        public WorldPoint Position
        {
            get { return _position; }
            set
            {
                _nextPosition = value;
                _positionChanged = true;
            }
        }

        /// <summary>The angle of the current orientation.</summary>
        public float Angle
        {
            get { return _angle; }
            set
            {
                Debug.Assert(!float.IsNaN(value));
                _nextAngle = value;
                _angleChanged = true;
            }
        }

        #endregion

        #region Fields

        /// <summary>The current translation of the component.</summary>
        private WorldPoint _position;

        /// <summary>The translation to move to when performing the next update.</summary>
        private WorldPoint _nextPosition;

        /// <summary>Don't rely on float equality checks.</summary>
        private bool _positionChanged;

        /// <summary>The current rotation of the component.</summary>
        private float _angle;

        /// <summary>The rotation to set to when performing the next update.</summary>
        private float _nextAngle;

        /// <summary>Don't rely on float equality checks.</summary>
        private bool _angleChanged;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified values.</summary>
        /// <param name="translation">The initial translation.</param>
        /// <param name="rotation">The initial rotation.</param>
        /// <returns>This component.</returns>
        public Transform Initialize(WorldPoint translation, float rotation = 0)
        {
            Position = translation;
            Angle = rotation;

            // Initialization must be called from a synchronous context (as
            // it must only be used when constructing the component). Thus
            // we want to trigger the update right now.
            Update();

            return this;
        }

        /// <summary>Initialize with the specified rotation.</summary>
        /// <param name="rotation">The initial rotation.</param>
        /// <returns></returns>
        public Transform Initialize(float rotation)
        {
            return Initialize(WorldPoint.Zero, rotation);
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            _position = WorldPoint.Zero;
            _nextPosition = WorldPoint.Zero;
            _positionChanged = false;
            _angle = 0;
            _nextAngle = 0;
            _angleChanged = false;
        }

        #endregion

        #region Methods
        
        /// <summary>
        ///     Applies the translation set to be used next. Called from system, because we want to keep the setter in debug
        ///     private to make sure no one actually writes directly to the translation variable.
        /// </summary>
        /// <remarks>This must be called from a synchronous context (i.e. not from a parallel system).</remarks>
        public void Update()
        {
            if (_positionChanged)
            {
                TranslationChanged message;
                message.Component = this;
                message.PreviousPosition = _position;
                message.CurrentPosition = _nextPosition;

                _position = _nextPosition;
                _positionChanged = false;

                Manager.SendMessage(message);
            }

            if (_angleChanged)
            {
                RotationChanged message;
                message.Component = this;
                message.PreviousRotation = _angle;
                message.CurrentRotation = _nextAngle;

                _angle = MathHelper.WrapAngle(_nextAngle);
                _angleChanged = false;

                Manager.SendMessage(message);
            }
        }

        #endregion
    }
}