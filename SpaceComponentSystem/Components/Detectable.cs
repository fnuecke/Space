using System;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.Serialization;
using JetBrains.Annotations;
using Microsoft.Xna.Framework.Graphics;

namespace Space.ComponentSystem.Components
{
    /// <summary>Marks an entity as detectable on the radar, which allows displaying it to the player and AI to react to it.</summary>
    public sealed class Detectable : Component
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
        
        #region Constants

        /// <summary>Index group to use for gravitational computations.</summary>
        public static readonly int IndexId = IndexSystem.GetIndexId();

        #endregion

        #region Properties

        /// <summary>The actual icon texture representing this detectable.</summary>
        [PublicAPI]
        public Texture2D Texture
        {
            get
            {
                if (_texture == null && !String.IsNullOrWhiteSpace(TextureName))
                {
                    var content = ((ContentSystem) Manager.GetSystem(ContentSystem.TypeId)).Content;
                    _texture = content.Load<Texture2D>(TextureName);
                }
                return _texture;
            }
        }

        /// <summary>The name of the texture to use for rendering the physics object.</summary>
        [PublicAPI]
        public string TextureName
        {
            get { return _textureName; }
            set
            {
                _textureName = value;
                _texture = null;
            }
        }

        #endregion

        #region Fields

        /// <summary>The actual texture with the set name.</summary>
        [PacketizeIgnore]
        private Texture2D _texture;

        /// <summary>Whether to use the objects rotation to rotate the detectable's icon.</summary>
        public bool RotateIcon;

        /// <summary>
        ///     Actual texture name. Setter is used to invalidate the actual texture reference, so we need to store this
        ///     ourselves.
        /// </summary>
        private string _textureName;

        #endregion

        #region Initialization

        /// <summary>Initialize with the specified texture name.</summary>
        /// <param name="textureName">Name of the texture.</param>
        /// <param name="rotateIcon">Whether to rotate the icon based on the object's rotation.</param>
        public Detectable Initialize(string textureName, bool rotateIcon = false)
        {
            TextureName = textureName;
            RotateIcon = rotateIcon;

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            _textureName = null;
            RotateIcon = false;
        }

        #endregion

        #region Serialization

        /// <summary>Bring the object to the state in the given packet.</summary>
        /// <param name="packet">The packet to read from.</param>
        public override void Depacketize(IReadablePacket packet)
        {
            base.Depacketize(packet);

            _texture = null;
        }

        #endregion
    }
}