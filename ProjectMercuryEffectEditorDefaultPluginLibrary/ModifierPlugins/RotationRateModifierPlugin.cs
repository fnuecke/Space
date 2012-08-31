/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.EffectEditor.DefaultPluginLibrary.ModifierPlugins
{
    using System;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using Microsoft.Xna.Framework;
    using ProjectMercury.EffectEditor.PluginInterfaces;
    using ProjectMercury.Modifiers;

    /// <summary>
    /// Defines a plugin which provided ColourModifier support to the EffectEditor.
    /// </summary>
    [Export(typeof(IModifierPlugin))]
    public class RotationRateModifierPlugin : IModifierPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name
        {
            get { return "Rotation Rate Modifier"; }
        }

        /// <summary>
        /// Gets the author of the plugin.
        /// </summary>
        public string Author
        {
            get { return "Matt Davey"; }
        }

        /// <summary>
        /// Gets the name of the plugin library, if any.
        /// </summary>
        public string Library
        {
            get { return "DefaultPluginLibrary"; }
        }

        /// <summary>
        /// Gets the version numbe of the plugin.
        /// </summary>
        public Version Version
        {
            get { return new Version(1, 0, 0, 0); }
        }

        /// <summary>
        /// Gets the minimum version of the engine with which the plugin is compatible.
        /// </summary>
        public Version MinimumRequiredVersion
        {
            get { return new Version(3, 1, 0, 0); }
        }

        /// <summary>
        /// Gets the display name for the Modifier type provided by the plugin.
        /// </summary>
        public string DisplayName
        {
            get { return "Rotation Rate Modifier"; }
        }

        /// <summary>
        /// Gets the description for the Modifier type provided by the plugin.
        /// </summary>
        public string Description
        {
            get { return "A Modifier which alters the rotation rate of a Particle over its lifetime."; }
        }

        /// <summary>
        /// Gets the icon to display for the Modifier type provided by the plugin.
        /// </summary>
        public Icon DisplayIcon
        {
            get { return Icons.Modifier; }
        }

        /// <summary>
        /// Gets the category of the modifier.
        /// </summary>
        /// <value></value>
        public string Category
        {
            get { return "Rotation"; }
        }

        /// <summary>
        /// Creates a default instance of the Modifier type provided by the plugin.
        /// </summary>
        /// <returns>An instance of the Modifier type provided by the plugin.</returns>
        public Modifier CreateDefaultInstance()
        {
            return new RotationRateModifier
            {
                InitialRate = MathHelper.PiOver2,
                FinalRate = 0f,
            };
        }
    }
}