using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;
using Nuclex.Input.Devices;
using Space.Input;

namespace Space.Util
{
    /// <summary>
    ///     This class keeps track of command bindings across multiple input devices, including mice, keyboards and
    ///     gamepads.
    /// </summary>
    /// <typeparam name="TCommand">The type of command an input maps to.</typeparam>
    public sealed class InputBindings<TCommand> : IXmlSerializable
        where TCommand : struct
    {
        #region Fields

        /// <summary>Command to input binding mapping.</summary>
        private readonly Dictionary<TCommand, Bindings> _bindings = new Dictionary<TCommand, Bindings>();

        /// <summary>Mouse button to command mapping.</summary>
        private readonly Dictionary<MouseButtons, TCommand> _mouseButtonBindings =
            new Dictionary<MouseButtons, TCommand>();

        /// <summary>Mouse wheel to command mapping.</summary>
        private readonly Dictionary<MouseWheel, TCommand> _mouseWheelBindings = new Dictionary<MouseWheel, TCommand>();

        /// <summary>Keyboard key to command mapping.</summary>
        private readonly Dictionary<Keys, TCommand> _keyboardBindings = new Dictionary<Keys, TCommand>();

        /// <summary>GamePad button to command mapping.</summary>
        private readonly Dictionary<Buttons, TCommand> _gamePadButtonBindings = new Dictionary<Buttons, TCommand>();

        /// <summary>GamePad axis to command mapping.</summary>
        private readonly Dictionary<ExtendedAxes, TCommand> _gamePadAxisBindings =
            new Dictionary<ExtendedAxes, TCommand>();

        #endregion

        #region Adding

        /// <summary>Add a new mouse button binding.</summary>
        /// <param name="command">The command that should be triggered.</param>
        /// <param name="button">The mouse button for which to trigger the command.</param>
        public void Add(TCommand command, MouseButtons button)
        {
            var bindings = GetBindings(command, true);
            if (bindings.MouseButtons.Contains(button))
            {
                return;
            }
            bindings.MouseButtons.Add(button);
            _mouseButtonBindings.Add(button, command);
        }

        /// <summary>Add a new mouse wheel binding.</summary>
        /// <param name="command">The command that should be triggered.</param>
        /// <param name="wheel">The mouse wheel direction for which to trigger the command.</param>
        public void Add(TCommand command, MouseWheel wheel)
        {
            var bindings = GetBindings(command, true);
            if (bindings.MouseWheel.Contains(wheel))
            {
                return;
            }
            bindings.MouseWheel.Add(wheel);
            _mouseWheelBindings.Add(wheel, command);
        }

        /// <summary>Add a new keyboard binding.</summary>
        /// <param name="command">The command that should be triggered.</param>
        /// <param name="key">The key for which to trigger the command.</param>
        public void Add(TCommand command, Keys key)
        {
            var bindings = GetBindings(command, true);
            if (bindings.KeyboardKeys.Contains(key))
            {
                return;
            }
            bindings.KeyboardKeys.Add(key);
            _keyboardBindings.Add(key, command);
        }

        /// <summary>Add a new gamepad button binding.</summary>
        /// <param name="command">The command that should be triggered.</param>
        /// <param name="button">The gamepad button for which to trigger the command.</param>
        public void Add(TCommand command, Buttons button)
        {
            var bindings = GetBindings(command, true);
            if (bindings.GamePadButtons.Contains(button))
            {
                return;
            }
            bindings.GamePadButtons.Add(button);
            _gamePadButtonBindings.Add(button, command);
        }

        /// <summary>Add a new gamepad axis binding.</summary>
        /// <param name="command">The command that should be triggered.</param>
        /// <param name="axis">The gamepad axis for which to trigger the command.</param>
        public void Add(TCommand command, ExtendedAxes axis)
        {
            var bindings = GetBindings(command, true);
            if (bindings.GamePadAxes.Contains(axis))
            {
                return;
            }
            bindings.GamePadAxes.Add(axis);
            _gamePadAxisBindings.Add(axis, command);
        }

        #endregion

        #region Removal

        /// <summary>Remove all bindings.</summary>
        public void Clear()
        {
            _bindings.Clear();
            _mouseButtonBindings.Clear();
            _mouseWheelBindings.Clear();
            _keyboardBindings.Clear();
            _gamePadButtonBindings.Clear();
            _gamePadAxisBindings.Clear();
        }

        #endregion

        #region Lookup

        /// <summary>Get the command assigned to the specified mouse button.</summary>
        /// <param name="button">The button to test.</param>
        /// <returns>The command, or the default value if there is no such binding.</returns>
        public TCommand GetCommand(MouseButtons button)
        {
            TCommand command;
            _mouseButtonBindings.TryGetValue(button, out command);
            return command;
        }

        /// <summary>Get the command assigned to the specified mouse wheel direction.</summary>
        /// <param name="wheel">The direction to test.</param>
        /// <returns>The command, or the default value if there is no such binding.</returns>
        public TCommand GetCommand(MouseWheel wheel)
        {
            TCommand command;
            _mouseWheelBindings.TryGetValue(wheel, out command);
            return command;
        }

        /// <summary>Get the command assigned to the specified key.</summary>
        /// <param name="key">The key to test.</param>
        /// <returns>The command, or the default value if there is no such binding.</returns>
        public TCommand GetCommand(Keys key)
        {
            TCommand command;
            _keyboardBindings.TryGetValue(key, out command);
            return command;
        }

        /// <summary>Get the command assigned to the specified gamepad axis.</summary>
        /// <param name="axis">The axis to test.</param>
        /// <returns>The command, or the default value if there is no such binding.</returns>
        public TCommand GetCommand(ExtendedAxes axis)
        {
            TCommand command;
            _gamePadAxisBindings.TryGetValue(axis, out command);
            return command;
        }

        /// <summary>Get the command assigned to the specified gamepad button.</summary>
        /// <param name="button">The button to test.</param>
        /// <returns>The command, or the default value if there is no such binding.</returns>
        public TCommand GetCommand(Buttons button)
        {
            TCommand command;
            _gamePadButtonBindings.TryGetValue(button, out command);
            return command;
        }

        /// <summary>Test if a specific command is currently active.</summary>
        /// <param name="command">The command to test for.</param>
        /// <param name="testCallback">A method that checks if a specified key is active (pressed).</param>
        /// <returns>Whether the command is active or not.</returns>
        public bool Test(TCommand command, Func<Keys, bool> testCallback)
        {
            // Get bindings for that command.
            Bindings bindings;
            _bindings.TryGetValue(command, out bindings);
            if (bindings == null)
            {
                return false;
            }

            // See if any single key for that command is active.
            return bindings.KeyboardKeys.Any(testCallback);
        }

        /// <summary>Get the maximum value of any axis that is mapped to the specified command.</summary>
        /// <param name="command">The command to test for.</param>
        /// <param name="getAxisValue">A method that returns the current value for the specified axis.</param>
        /// <returns>The maximum of any tested axis.</returns>
        public float Test(TCommand command, Func<ExtendedAxes, float> getAxisValue)
        {
            // Get bindings for that command.
            Bindings bindings;
            _bindings.TryGetValue(command, out bindings);
            if (bindings == null)
            {
                return 0;
            }

            // See if any single axis for that command is active, get the maximum.
            float value = 0;
            foreach (var axis in bindings.GamePadAxes)
            {
                var axisValue = getAxisValue(axis);
                if (Math.Abs(axisValue) > Math.Abs(value))
                {
                    value = axisValue;
                }
            }
            return value;
        }

        #endregion

        #region Serialization

        /// <summary>This method is reserved and should not be used.</summary>
        /// <returns>
        ///     <code>null</code>
        /// </returns>
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        /// <summary>Converts an object into its XML representation.</summary>
        /// <param name="writer">
        ///     The <see cref="T:System.Xml.XmlWriter"/> stream to which the object is serialized.
        /// </param>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            foreach (var pair in _bindings)
            {
                // Write one element per binding.
                writer.WriteStartElement("Binding");

                // Write the command as an attribute.
                writer.WriteAttributeString("Command", pair.Key.ToString());

                // Write all bindings as child elements.
                foreach (var button in pair.Value.MouseButtons)
                {
                    writer.WriteElementString("Mouse", button.ToString());
                }
                foreach (var wheel in pair.Value.MouseWheel)
                {
                    writer.WriteElementString("Wheel", wheel.ToString());
                }
                foreach (var key in pair.Value.KeyboardKeys)
                {
                    writer.WriteElementString("Key", key.ToString());
                }
                foreach (var button in pair.Value.GamePadButtons)
                {
                    writer.WriteElementString("GamePad", button.ToString());
                }
                foreach (var axis in pair.Value.GamePadAxes)
                {
                    writer.WriteElementString("Axis", axis.ToString());
                }

                // Finish the element.
                writer.WriteEndElement();
            }
        }

        /// <summary>Generates an object from its XML representation.</summary>
        /// <param name="reader">
        ///     The <see cref="T:System.Xml.XmlReader"/> stream from which the object is deserialized.
        /// </param>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            // Check if we have anything at all.
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }

            // Consume start of wrapper.
            reader.ReadStartElement();

            // Start parsing child elements.
            while (reader.MoveToContent() == XmlNodeType.Element)
            {
                // Skip elements we're not interested in.
                if (!reader.Name.Equals("Binding"))
                {
                    // Not something we care about.
                    reader.Read();
                    continue;
                }

                // Read input binding elements. Get the command type.
                var commandName = reader["Command"];
                TCommand command;
                if (commandName == null || !Enum.TryParse(commandName, true, out command))
                {
                    // Invalid command, skip this block.
                    reader.Read();
                    continue;
                }

                // Skip if empty.
                if (reader.IsEmptyElement)
                {
                    reader.Read();
                    continue;
                }

                // Consume start of 'Bindings'.
                reader.ReadStartElement();

                // Get all bindings.
                while (reader.MoveToContent() == XmlNodeType.Element)
                {
                    // Skip empty elements.
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                        continue;
                    }

                    // See what we've got.
                    if (reader.Name.Equals("Mouse", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Got a mouse button binding.
                        var value = reader.ReadElementString();
                        if (value != null)
                        {
                            MouseButtons button;
                            if (Enum.TryParse(value, true, out button))
                            {
                                Add(command, button);
                            }
                        }
                    }
                    else if (reader.Name.Equals("Wheel", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Got a mouse wheel binding.
                        var value = reader.ReadElementString();
                        if (value != null)
                        {
                            MouseWheel wheel;
                            if (Enum.TryParse(value, true, out wheel))
                            {
                                Add(command, wheel);
                            }
                        }
                    }
                    else if (reader.Name.Equals("Key", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Got a key binding.
                        var value = reader.ReadElementString();
                        if (value != null)
                        {
                            Keys key;
                            if (Enum.TryParse(value, true, out key))
                            {
                                Add(command, key);
                            }
                        }
                    }
                    else if (reader.Name.Equals("GamePad", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Got a mouse binding.
                        var value = reader.ReadElementString();
                        if (value != null)
                        {
                            Buttons button;
                            if (Enum.TryParse(value, true, out button))
                            {
                                Add(command, button);
                            }
                        }
                    }
                    else if (reader.Name.Equals("Axis", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Got a mouse binding.
                        var value = reader.ReadElementString();
                        if (value != null)
                        {
                            ExtendedAxes axis;
                            if (Enum.TryParse(value, true, out axis))
                            {
                                Add(command, axis);
                            }
                        }
                    }
                    else
                    {
                        // Unknown element, just consume it.
                        reader.Read();
                    }
                }

                // Consume the end of 'Bindings'.
                reader.ReadEndElement();
            }

            // Consume the end of our wrapper.
            reader.ReadEndElement();
        }

        #endregion

        #region Utility methods

        /// <summary>Utility method for getting bindings for a specific command.</summary>
        /// <param name="command">The command to look the bindings up for.</param>
        /// <param name="create">Whether to create the bindings if they don't already exist.</param>
        /// <returns>The bindings for that command.</returns>
        private Bindings GetBindings(TCommand command, bool create = false)
        {
            Bindings bindings;
            _bindings.TryGetValue(command, out bindings);
            if (bindings == null && create)
            {
                bindings = new Bindings();
                _bindings.Add(command, bindings);
            }
            return bindings;
        }

        #endregion

        #region Utility types

        /// <summary>Utility class for mapping commands to bindings. Easier to read than when using a Tuple.</summary>
        private sealed class Bindings
        {
            public readonly List<MouseButtons> MouseButtons = new List<MouseButtons>();

            public readonly List<MouseWheel> MouseWheel = new List<MouseWheel>();

            public readonly List<Keys> KeyboardKeys = new List<Keys>();

            public readonly List<Buttons> GamePadButtons = new List<Buttons>();

            public readonly List<ExtendedAxes> GamePadAxes = new List<ExtendedAxes>();
        }

        #endregion
    }
}