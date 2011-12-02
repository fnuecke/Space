using System;
using Engine.Session;

namespace Engine.Serialization
{
    /// <summary>
    /// Base interface all packetizer contexts must implement. This defines
    /// information the engine itself needs internally.
    /// </summary>
    /// <typeparam name="TPlayerData"></typeparam>
    /// <typeparam name="TPacketizerContext"></typeparam>
    public interface IPacketizerContext<TPlayerData, TPacketizerContext> : ICloneable
        where TPlayerData : IPacketizable<TPlayerData, TPacketizerContext>
        where TPacketizerContext : IPacketizerContext<TPlayerData, TPacketizerContext>
    {
        /// <summary>
        /// The session the packetizer context is bound to.
        /// </summary>
        ISession<TPlayerData, TPacketizerContext> Session { get; set; }
    }
}
