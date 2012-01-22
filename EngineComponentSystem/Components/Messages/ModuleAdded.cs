using Engine.ComponentSystem.Modules;

namespace Engine.ComponentSystem.Components.Messages
{
    /// <summary>
    /// Sent by <c>EntityModules</c> when a new module was added.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute types.</typeparam>
    public struct ModuleAdded<TAttribute>
        where TAttribute : struct
    {
        /// <summary>
        /// The module that was added.
        /// </summary>
        public AbstractModule<TAttribute> Module;
    }
}
