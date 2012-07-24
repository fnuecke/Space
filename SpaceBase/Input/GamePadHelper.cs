using System;
using Microsoft.Xna.Framework;
using Nuclex.Input.Devices;
using Space.Util;

namespace Space.Input
{
    /// <summary>
    /// Utility class, used for evaluating game pad state based on the current
    /// game settings.
    /// </summary>
    public static class GamePadHelper
    {
        /// <summary>
        /// Gets the current acceleration vector.
        /// </summary>
        /// <param name="gamePad">The gamepad to read the state from.</param>
        /// <returns>The current acceleration direction.</returns>
        public static Vector2 GetAcceleration(IGamePad gamePad)
        {
            Vector2 result;

            // Get X and Y acceleration.
            result.X = ComputeAxis(gamePad, GamePadCommand.AccelerateX);
            result.Y = ComputeAxis(gamePad, GamePadCommand.AccelerateY);

            // See if we want to invert any axis.
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

            // Get X and Y look direction.
            result.X = ComputeAxis(gamePad, GamePadCommand.LookX);
            result.Y = ComputeAxis(gamePad, GamePadCommand.LookY);

            // See if we want to invert any axis.
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
        private static float ComputeAxis(IGamePad gamePad, GamePadCommand command)
        {
            var state = gamePad.GetExtendedState();
            var value = Settings.Instance.AxisBindings.Test(command, state.GetAxis);
            if (Math.Abs(value) < Settings.Instance.GamePadDetectionEpsilon)
            {
                value = 0;
            }
            return value;
        }
    }
}
