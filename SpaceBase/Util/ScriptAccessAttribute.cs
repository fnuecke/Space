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

        /// <summary>
        /// Possible value for this setting, if it has a set of discrete values.
        /// </summary>
        public object[] Options { get; set; }

        /// <summary>
        /// Minimum valid value, for numeric types.
        /// </summary>
        public object MinValue { get; set; }

        /// <summary>
        /// Maximum valid value, for numeric types.
        /// </summary>
        public object MaxValue { get; set; }

        /// <summary>
        /// Whether the setting should be listed in the options screen.
        /// </summary>
        public bool ShouldList { get; set; }

        /// <summary>
        /// The name of the resource with the localized title for this setting.
        /// </summary>
        public string TitleLocalizationId { get; set; }

        /// <summary>
        /// The name of the resource with the localized description for this setting.
        /// </summary>
        public string DescriptionLocalizationId { get; set; }

        internal ScriptAccessAttribute(string name)
        {
            Name = name;
            ShouldList = true;
            TitleLocalizationId = Name;
            DescriptionLocalizationId = Name + "Description";
        }
    }
}
