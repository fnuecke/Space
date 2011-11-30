using System;
using Engine.Serialization;

namespace Engine.Simulation
{
    /// <summary>
    /// Interface to steppable factories, as used in states.
    /// </summary>
    public interface ISteppableFactory<TState, TSteppable, TCommandType, TPlayerData, TPacketizerContext> : ICloneable, IPacketizable<TPacketizerContext>
    {
        /// <summary>
        /// Gets a unique ID for a steppable. Code pattern should look like this:
        /// <example>
        /// <code>
        /// var obj = factory.GetUniqueId(new Blah(...));
        /// </code>
        /// </example>
        /// </summary>
        /// <typeparam name="T">the type of steppable.</typeparam>
        /// <param name="value">the object for which to get a unique id.</param>
        /// <returns>the same steppable instance, now with a unique id.</returns>
        T GetUniqueId<T>(T value) where T : TSteppable;

        /// <summary>
        /// Simply increment the internal counter by one. This is useful if
        /// a state gets an existing object, which already has an id.
        /// </summary>
        void Increment();
    }
}
