using Microsoft.Xna.Framework;

namespace Engine.ComponentSystem.Components.Messages
{
    public struct TranslationChanged
    {
        public Vector2 PreviousPosition;

        public static TranslationChanged Create(Vector2 previousPosition)
        {
            TranslationChanged result;
            result.PreviousPosition = previousPosition;
            return result;
        }
    }
}
