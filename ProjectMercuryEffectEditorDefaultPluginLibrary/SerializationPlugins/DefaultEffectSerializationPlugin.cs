/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.EffectEditor.DefaultPluginLibrary.SerializationPlugins
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
    using ProjectMercury.EffectEditor.PluginInterfaces;

    [Export(typeof(IEffectSerializationPlugin))]
    public class DefaultEffectSerializationPlugin : IEffectSerializationPlugin
    {
        #region IEffectSerializationPlugin Members

        /// <summary>
        /// Gets the filter to use in th file dialog when the serialization plugin is selected.
        /// ie: "Particle Effect Files|*.pfx"
        /// </summary>
        /// <value></value>
        public string FileFilter
        {
            get { return "Particle Effect (*.xml)|*.xml"; }
        }

        /// <summary>
        /// Gets a value indicating wether or not serialization is supported by this plugin.
        /// </summary>
        /// <value></value>
        public bool SerializeSuported
        {
            get { return true; }
        }

        /// <summary>
        /// Serializes the specified ParticleEffect instance.
        /// </summary>
        /// <param name="effect">The ParticleEffect to be serialized.</param>
        /// <param name="filename">The desired output file name,</param>
        public void Serialize(ParticleEffect effect, string filename)
        {
            // Create a new xml document...
            XDocument xmlDocument = new XDocument();

            // Use the XNA serializer to populate the xml document...
            using (XmlWriter writer = xmlDocument.CreateWriter())
            {
                IntermediateSerializer.Serialize<ParticleEffect>(writer, effect, ".\\");
            }

            //// hack: Workaround for intermediate serializer not putting nodes in the right order...
            //foreach (XElement emitterElement in xmlDocument.Descendants("Asset").Elements("Item"))
            //{
            //    XElement releaseQuantityElement = emitterElement.Element("ReleaseQuantity");

            //    if ((releaseQuantityElement.PreviousNode as XElement).Name == "Name")
            //    {
            //        XElement termElement = emitterElement.Element("Term");

            //        termElement.AddAfterSelf(releaseQuantityElement);

            //        releaseQuantityElement.Remove();
            //    }
            //}

            // Save the xml document...
            xmlDocument.Save(filename);
        }

        /// <summary>
        /// Gets a value indicating wether or not deserialization is supported by this plugin.
        /// </summary>
        /// <value></value>
        public bool DeserializeSupported
        {
            get { return true; }
        }

        /// <summary>
        /// Deserializes the specified ParticleEffect file.
        /// </summary>
        /// <param name="filename">The desired input file name.</param>
        /// <returns>A deserialized ParticleEffect instance.</returns>
        public ParticleEffect Deserialize(string filename)
        {
            // load the xml document...
            XDocument xmlDocument = XDocument.Load(filename);

            //try
            //{
            using (XmlReader reader = xmlDocument.CreateReader())
            {
                return IntermediateSerializer.Deserialize<ParticleEffect>(reader, ".\\");
            }
            //}
            //catch
            //{
            //    // hack: workaround for intermediate serializer not putting nodes in the right order...
            //    foreach (XElement emitterElement in xmlDocument.Descendants("Asset").Elements("Item"))
            //    {
            //        XElement releaseQuantityElement = emitterElement.Element("ReleaseQuantity");

            //        if ((releaseQuantityElement.PreviousNode as XElement).Name == "Name")
            //        {
            //            XElement termElement = emitterElement.Element("Term");

            //            termElement.AddAfterSelf(releaseQuantityElement);

            //            releaseQuantityElement.Remove();
            //        }
            //    }

            //    using (XmlReader reader = xmlDocument.CreateReader())
            //    {
            //        return IntermediateSerializer.Deserialize<ParticleEffect>(reader, ".\\");
            //    }
            //}
        }

        #endregion

        #region IPlugin Members

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        /// <value></value>
        public string Name
        {
            get { return "Default Effect Serializer"; }
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName
        {
            get { return "Mercury Particle Engine XML (*.xml)"; }
        }

        /// <summary>
        /// Gets the display icon.
        /// </summary>
        /// <value>The display icon.</value>
        public Icon DisplayIcon
        {
            get { return Icons.ParticleEffect; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Open or save a Particle Effect in the default Mercury Particle Engine format."; }
        }

        /// <summary>
        /// Gets the author of the plugin.
        /// </summary>
        /// <value></value>
        public string Author
        {
            get { return "Matt Davey"; }
        }

        /// <summary>
        /// Gets the name of the plugin library, if any.
        /// </summary>
        /// <value></value>
        public string Library
        {
            get { return "DefaultPluginLibrary"; }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value></value>
        public Version Version
        {
            get { return new Version(1, 0, 0, 0); }
        }

        /// <summary>
        /// Gets the minimum version of the engine with which the plugin is compatible.
        /// </summary>
        /// <value></value>
        public Version MinimumRequiredVersion
        {
            get { return new Version(3, 1, 0, 0); }
        }

        #endregion
    }
}
