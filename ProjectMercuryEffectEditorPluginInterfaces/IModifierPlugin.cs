/*  
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.EffectEditor.PluginInterfaces
{
    using ProjectMercury.Modifiers;

    /// <summary>
    /// Defines the interface for a Modifier plugin.
    /// </summary>
    public interface IModifierPlugin :IPlugin
    {
        /// <summary>
        /// Gets the category of the modifier.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Creates a default instance of the Modifier type provided by the plugin.
        /// </summary>
        /// <returns>An instance of the Modifier type provided by the plugin.</returns>
        Modifier CreateDefaultInstance();
    }
}