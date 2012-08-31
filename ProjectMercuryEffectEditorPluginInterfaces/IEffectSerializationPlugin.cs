/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.EffectEditor.PluginInterfaces
{
    using System;
    using System.Drawing;

    /// <summary>
    /// Defines the interface for a ParticleEffect serialization plugin.
    /// </summary>
    public interface IEffectSerializationPlugin : IPlugin
    {
        /// <summary>
        /// Gets the filter to use in th file dialog when the serialization plugin is selected.
        /// ie: "Particle Effect Files|*.pfx"
        /// </summary>
        string FileFilter { get; }

        /// <summary>
        /// Gets a value indicating wether or not serialization is supported by this plugin.
        /// </summary>
        bool SerializeSuported { get; }

        /// <summary>
        /// Serializes the specified ParticleEffect instance.
        /// </summary>
        /// <param name="effect">The ParticleEffect to be serialized.</param>
        /// <param name="filename">The desired output file name,</param>
        void Serialize(ParticleEffect effect, string filename);

        /// <summary>
        /// Gets a value indicating wether or not deserialization is supported by this plugin.
        /// </summary>
        bool DeserializeSupported { get; }

        /// <summary>
        /// Deserializes the specified ParticleEffect file.
        /// </summary>
        /// <param name="filename">The desired input file name.</param>
        /// <returns>A deserialized ParticleEffect instance.</returns>
        ParticleEffect Deserialize(string filename);
    }
}