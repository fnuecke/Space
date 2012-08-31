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
    using ProjectMercury.EffectEditor.PluginInterfaces;
    using ProjectMercury.Modifiers;

    [Export(typeof(IModifierPlugin))]
    public class VelocityClampModifierPlugin : IModifierPlugin
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Velocity Clamp Modifier"; }
        }

        /// <summary>
        /// Gets the author.
        /// </summary>
        /// <value>The author.</value>
        public string Author
        {
            get { return "Matt Davey"; }
        }

        /// <summary>
        /// Gets the library.
        /// </summary>
        /// <value>The library.</value>
        public string Library
        {
            get { return "DefaultPluginLibrary"; }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public Version Version
        {
            get { return new Version(1, 0, 0, 0); }
        }

        /// <summary>
        /// Gets the minimum required version.
        /// </summary>
        /// <value>The minimum required version.</value>
        public Version MinimumRequiredVersion
        {
            get { return new Version(3, 1, 0, 0); }
        }

        /// <summary>
        /// Gets the display name for the Modifier type provided by the plugin.
        /// </summary>
        /// <value></value>
        public string DisplayName
        {
            get { return "Velocity Clamp Modifier"; }
        }

        /// <summary>
        /// Gets the description for the Modifier type provided by the plugin.
        /// </summary>
        /// <value></value>
        public string Description
        {
            get { return "A Modifier which limits the velocity of Particles. to a specified value."; }
        }

        /// <summary>
        /// Gets the displayicon.
        /// </summary>
        /// <value>The displayicon.</value>
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
            get { return "Forces & Deflectors"; }
        }

        /// <summary>
        /// Creates a default instance of the Modifier type provided by the plugin.
        /// </summary>
        /// <returns>
        /// An instance of the Modifier type provided by the plugin.
        /// </returns>
        public Modifier CreateDefaultInstance()
        {
            return new VelocityClampModifier
            {
                MaximumVelocity = 50f
            };
        }
    }
}