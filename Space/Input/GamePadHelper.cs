using Microsoft.Xna.Framework;
using Nuclex.Input.Devices;

namespace Space.Input
{
    static class GamePadHelper
    {
        public static Vector2 GetAcceleration(IGamePad gamePad)
        {
            Vector2 result;
            result.X = ComputeAxis(gamePad, Settings.GamePadCommand.AccelerateX);
            result.Y = ComputeAxis(gamePad, Settings.GamePadCommand.AccelerateY);
            return result;
        }

        public static Vector2 GetLook(IGamePad gamePad)
        {
            Vector2 result;
            result.X = ComputeAxis(gamePad, Settings.GamePadCommand.LookX);
            result.Y = ComputeAxis(gamePad, Settings.GamePadCommand.LookY);
            return result;
        }

        private static float ComputeAxis(IGamePad gamePad, Settings.GamePadCommand command)
        {
            if (Settings.Instance.InverseGamePadBindings.ContainsKey(command))
            {
                var state = gamePad.GetExtendedState();
                float value = 0;
                foreach (var axis in Settings.Instance.InverseGamePadBindings[command])
                {
                    value += state.GetAxis(axis);
                }
                if (System.Math.Abs(value) < Settings.Instance.GamePadDetectionEpsilon)
                {
                    value = 0;
                }
                return value;
            }
            return 0;
        }
    }
}
