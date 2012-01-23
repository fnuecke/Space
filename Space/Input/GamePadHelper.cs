using Microsoft.Xna.Framework;
using Nuclex.Input.Devices;

namespace Space.Input
{
    /// <summary>
    /// Utility class, used for evaluating game pad state based on the current
    /// game settings.
    /// </summary>
    static class GamePadHelper
    {
        /// <summary>
        /// Gets the current acceleration vector.
        /// </summary>
        /// <param name="gamePad">The gamepad to read the state from.</param>
        /// <returns>The current acceleration direction.</returns>
        public static Vector2 GetAcceleration(IGamePad gamePad)
        {
            Vector2 result;

            result.X = ComputeAxis(gamePad, Settings.GamePadCommand.AccelerateX);
            result.Y = ComputeAxis(gamePad, Settings.GamePadCommand.AccelerateY);

            if (Settings.Instance.InvertGamepadAccelerationAxisX)
            {
                result.X = -result.X;
            }
            if (Settings.Instance.InvertGamepadAccelerationAxisY)
            {
                result.Y = -result.Y;
            }

            return result;
        }

        /// <summary>
        /// Gets the current look vector.
        /// </summary>
        /// <param name="gamePad">The gamepad to read the state from.</param>
        /// <returns>The current look direction.</returns>
        public static Vector2 GetLook(IGamePad gamePad)
        {
            Vector2 result;

            result.X = ComputeAxis(gamePad, Settings.GamePadCommand.LookX);
            result.Y = ComputeAxis(gamePad, Settings.GamePadCommand.LookY);

            if (Settings.Instance.InvertGamepadLookAxisX)
            {
                result.X = -result.X;
            }
            if (Settings.Instance.InvertGamepadLookAxisY)
            {
                result.Y = -result.Y;
            }

            return result;
        }

        /// <summary>
        /// Actual evaluation of axii based on game settings.
        /// </summary>
        /// <param name="gamePad">The gamepad to read the state from.</param>
        /// <param name="command">The command / axis for which to get the value.</param>
        /// <returns>The value along that single axis.</returns>
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
