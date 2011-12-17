using System;
using Engine.Serialization;
using Engine.Util;

namespace Engine.ComponentSystem.Components
{
    public interface IComponent : ICloneable, IPacketizable, IHashable
    {
        void Update(object parameterization);

        bool SupportsParameterization(Type parameterizationType);
    }
}
