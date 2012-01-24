namespace Engine.ComponentSystem.Messages
{
    /// <summary>
    /// Sent when an attribute value of a module was changed directly.
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    public struct ModuleValueInvalidated<TAttribute>
        where TAttribute : struct
    {
        /// <summary>
        /// The type of the value that changed.
        /// </summary>
        public TAttribute ValueType;
    }
}
