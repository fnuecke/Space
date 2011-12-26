using Engine.Math;

namespace Engine.ComponentSystem.Components.Messages
{
    public struct TranslationChanged
    {
        public FPoint PreviousPosition;

        public static TranslationChanged Create(FPoint previousPosition)
        {
            TranslationChanged result;
            result.PreviousPosition = previousPosition;
            return result;
        }
    }
}
