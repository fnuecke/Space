using System;

namespace Space.Util
{
    /// <summary>
    /// Attribute used to mark entries in the settings that should be exposed
    /// to the GUI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ScriptAccessAttribute : Attribute
    {
        /// <summary>
        /// The name under which the setting will be available in scripting.
        /// </summary>
        public readonly string Name;

        internal ScriptAccessAttribute(string name)
        {
            Name = name;
        }
    }
}
