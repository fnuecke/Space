﻿using System.ComponentModel;
using Engine.ComponentSystem;
using Engine.Random;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Design;
using Space.ComponentSystem.Components;

namespace Space.ComponentSystem.Factories
{
    /// <summary>Constraints for generating shields.</summary>
    public sealed class ShieldFactory : ItemFactory
    {
        #region Properties

        /// <summary>Gets or sets the structure texture.</summary>
        [Editor("Space.Tools.DataEditor.TextureAssetEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [DefaultValue(null)]
        [ContentSerializer(Optional = true)]
        [Category("Media")]
        [Description("The texture to use as a structure for the shield's shader.")]
        public string Structure
        {
            get { return _structure; }
            set { _structure = value; }
        }

        /// <summary>The color tint for generated shields.</summary>
        [Editor("Space.Tools.DataEditor.XnaColorEditor, Space.Tools.DataEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [TypeConverter(typeof (ColorConverter))]
        [ContentSerializer(Optional = true)]
        [DefaultValue(0xFFFFFFFF)]
        [Category("Media")]
        [Description("The color tint to apply to the shader.")]
        public Color Tint
        {
            get { return _tint; }
            set { _tint = value; }
        }

        #endregion

        #region Backing fields

        private string _structure;

        private Color _tint = Color.White;

        #endregion

        #region Sampling

        /// <summary>Samples a new shield based on these constraints.</summary>
        /// <param name="manager"></param>
        /// <param name="random">The randomizer to use.</param>
        /// <returns>The sampled shield.</returns>
        public override int Sample(IManager manager, IUniformRandom random)
        {
            var entity = base.Sample(manager, random);

            manager.AddComponent<Shield>(entity)
                   .Initialize(this)
                   .Initialize(Name, Icon, Quality, RequiredSlotSize, ModelOffset, ModelBelowParent);

            return entity;
        }

        #endregion
    }
}