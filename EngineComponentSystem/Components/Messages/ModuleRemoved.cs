using Engine.ComponentSystem.Modules;

namespace Engine.ComponentSystem.Components.Messages
{
    /// <summary>
    /// Sent by <c>EntityModules</c> when a module was removed.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute types.</typeparam>
    public struct ModuleRemoved<TAttribute>
        where TAttribute : struct
    {
        /// <summary>
        /// The module that was removed.
        /// </summary>
        public AbstractModule<TAttribute> Module;
    }
}
