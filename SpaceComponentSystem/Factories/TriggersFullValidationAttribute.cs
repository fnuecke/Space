using System;

namespace Space.ComponentSystem.Factories
{
    /// <summary>Properties marked with this attribute will trigger a full revalidation in the editor.</summary>
    public sealed class TriggersFullValidationAttribute : Attribute {}
}