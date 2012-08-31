/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.EffectEditor.DefaultPluginLibrary.Emitterplugins
{
    using System;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using Microsoft.Xna.Framework;
    using ProjectMercury.EffectEditor.PluginInterfaces;
    using ProjectMercury.Emitters;

    [Export(typeof(IEmitterPlugin))]
    public class CircleEmitterPlugin : IEmitterPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name
        {
            get { return "Circle Emitter"; }
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
        /// Gets the version number of the plugin.
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
        /// Gets the display name for the Emitter type provided by the plugin.
        /// </summary>
        public string DisplayName
        {
            get { return "Circle Emitter"; }
        }

        /// <summary>
        /// Gets the description for the Emitter type provided by the plugin.
        /// </summary>
        public string Description
        {
            get { return "An Emitter which releases Particles in a circle or ring shape."; }
        }

        /// <summary>
        /// Gets the icon to display for the Emitter type provided by the plugin.
        /// </summary>
        public Icon DisplayIcon
        {
            get { return Icons.Emitter; }
        }

        /// <summary>
        /// Creates a default instance of the Emitter type provided by the plugin.
        /// </summary>
        /// <returns>An instance of the Emitter type provided by the plugin.</returns>
        public Emitter CreateDefaultInstance()
        {
            return new CircleEmitter
            {
                Budget = 1000,
                ReleaseColour = Vector3.One,
                ReleaseOpacity = 1f,
                ReleaseQuantity = 1,
                ReleaseRotation = 0f,
                ReleaseScale = 32f,
                ReleaseSpeed = new VariableFloat { Variation = 25f },
                Term = 2f,

                Radiate = true,
                Radius = 50f,
                Ring = true
            };
        }
    }
}