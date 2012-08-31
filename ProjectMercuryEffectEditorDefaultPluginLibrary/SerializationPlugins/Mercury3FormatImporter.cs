/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.EffectEditor.DefaultPluginLibrary.SerializationPlugins
{
    using System;
    using System.ComponentModel.Composition;
    using System.Drawing;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
    using ProjectMercury.EffectEditor.PluginInterfaces;

    [Export(typeof(IEffectSerializationPlugin))]
    public class Mercury3FormatImporter : IEffectSerializationPlugin
    {
        #region IEffectSerializationPlugin Members

        /// <summary>
        /// Gets the filter to use in th file dialog when the serialization plugin is selected.
        /// ie: "Particle Effect Files|*.pfx"
        /// </summary>
        /// <value></value>
        public string FileFilter
        {
            get { return "Emitter Definition (*.em)|*.em"; }
        }

        /// <summary>
        /// Gets a value indicating wether or not serialization is supported by this plugin.
        /// </summary>
        /// <value></value>
        public bool SerializeSuported
        {
            get { return false; }
        }

        /// <summary>
        /// Serializes the specified ParticleEffect instance.
        /// </summary>
        /// <param name="effect">The ParticleEffect to be serialized.</param>
        /// <param name="filename">The desired output file name,</param>
        public void Serialize(ParticleEffect effect, string filename)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets a value indicating wether or not deserialization is supported by this plugin.
        /// </summary>
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
            XDocument inputDocument = XDocument.Load(filename);

            XElement assetElement = inputDocument.Root.Element("Asset");

            // Add 'Name' element...
            assetElement.AddFirst(new XElement("Name", "Imported Emitter"));

            // Add 'Enabled' element...
            XElement termElement = assetElement.Element("Term");

            termElement.AddAfterSelf(new XElement("Enabled", "true"));

            // Add 'BlendMode' element...
            XElement modifiersElement = assetElement.Element("Modifiers");

            modifiersElement.AddAfterSelf(new XElement("BlendMode", "Add"));

            // Adjust VariableFloat elements...
            foreach (XElement variableFloatElement in (from XElement e in assetElement.Descendants()
                                                       where e.Element("Anchor") != null &&
                                                             e.Element("Anchor").Value.Contains(" ") == false &&
                                                             e.Element("Variation") != null
                                                       select e))
            {
                variableFloatElement.Element("Anchor").Name = "Value";

                float anchorValue = float.Parse(variableFloatElement.Element("Value").Value);

                float oldVariation = float.Parse(variableFloatElement.Element("Variation").Value);

                float newVariation = anchorValue * oldVariation;

                variableFloatElement.Element("Variation").Value = newVariation.ToString();
            }

            // Adjust ReleaseColour element...
            XElement releaseColourElement = assetElement.Element("ReleaseColour");

            string oldColour = releaseColourElement.Value;

            releaseColourElement.Value = String.Empty;

            releaseColourElement.Add(new XElement("Value", oldColour),
                                     new XElement("Variation", "0 0 0"));

            // Add 'ReleaseImpulse' element...
            XElement releaseRotationElement = assetElement.Element("ReleaseRotation");

            releaseRotationElement.AddAfterSelf(new XElement("ReleaseImpulse", "0 0"));

            // Add 'TriggerOffset' and 'MinimumTriggerPeriod' elements...
            XElement blendModeElement = assetElement.Element("BlendMode");

            blendModeElement.AddAfterSelf(new XElement("TriggerOffset", "0 0"),
                                          new XElement("MinimumTriggerPeriod", "0"));

            // Reorder 'ReleaseQuantity' element...
            XElement releaseQuantityElement = assetElement.Element("ReleaseQuantity");
            releaseQuantityElement.Remove();
            termElement.AddAfterSelf(releaseQuantityElement);

            // Rename ColorModifier to ColourModifier...
            foreach (XElement colorElement in (from element in assetElement.Element("Modifiers").Elements("Item")
                                               where element.Attribute("Type").Value.Contains("ColorModifier")
                                               select element))
            {
                colorElement.Attribute("Type").Value = "Modifiers:ColourModifier";
            };

            // Remove RandomColourModifier elements...
            foreach (XElement randomElement in (from element in assetElement.Element("Modifiers").Elements("Item")
                                                where element.Attribute("Type").Value.Contains("RandomColourModifier")
                                                select element))
            {
                randomElement.Remove();
            };

            // Add InnerRadius property to any RadialGravityModifier elements...
            foreach (XElement radialElement in (from element in assetElement.Element("Modifiers").Elements("Item")
                                                where element.Attribute("Type").Value.Contains("RadialGravityModifier")
                                                select element))
            {
                XElement radiusElement = radialElement.Element("Radius");

                radiusElement.AddAfterSelf(new XElement("InnerRadius", 0f));
            }

            // Add a Rotation element for RectEmitter elements...
            if (assetElement.Attribute("Type").Value.Contains("RectEmitter"))
            {
                XElement heightElement = assetElement.Element("Height");

                heightElement.AddAfterSelf(new XElement("Rotation", 0f));
            }

            // Wrap emitter element in a new ParticleEffect element...
            assetElement.Name = "Item";
            assetElement.Remove();

            // Create new Asset element...
            XElement newAssetElement = new XElement("Asset", new XAttribute("Type", "ProjectMercury.ParticleEffect"),
                                                             assetElement);

            inputDocument.Element("XnaContent").Add(newAssetElement);

            inputDocument.Save("TRANSFORMED.XML");

            return IntermediateSerializer.Deserialize<ParticleEffect>(inputDocument.CreateReader(), "..\\");
        }

        #endregion

        #region IPlugin Members

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string Name
        {
            get { return "Mercury 3 Format Importer"; }
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName
        {
            get { return "Mercury 3.0 Emitter Format (*.em)"; }
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
            get { return "Open an Emitter definition file from Mercury Particle Engine 3.0"; }
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

        #endregion
    }
}