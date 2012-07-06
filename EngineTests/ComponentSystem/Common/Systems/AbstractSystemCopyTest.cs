using Engine.ComponentSystem;
using Engine.ComponentSystem.Systems;
using Engine.Tests.Base.Util;

namespace Engine.Tests.ComponentSystem.Common.Systems
{
    public abstract class AbstractSystemCopyTest : AbstractCopyableTest<AbstractSystem>
    {
        protected override void InitCopy(AbstractSystem copy)
        {
            copy.Manager = new Manager();
        }
    }
}
