using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Engine.Input
{

    /// <summary>
    /// Utility class to map XNA's key representation (the <see cref="Microsoft.Xna.Framework.Input.Keys"/> enum) to their char representation.
    /// </summary>
    public sealed class KeyMap
    {

        #region Instance

        #region Fields

        /// <summary>
        /// Mapping of modifiers to keys to chars.
        /// </summary>
        private Dictionary<KeyModifier, Dictionary<Keys, char>> mapping = new Dictionary<KeyModifier, Dictionary<Keys, char>>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new, empty key map.
        /// </summary>
        public KeyMap()
        {
            foreach (var modifier in (KeyModifier[])Enum.GetValues(typeof(KeyModifier)))
            {
                mapping.Add(modifier, new Dictionary<Keys, char>());
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Adds a mapping to this key map.
        /// </summary>
        /// <param name="modifier">the modifier this mapping applies to.</param>
        /// <param name="key">the key to map.</param>
        /// <param name="ch">the char to map to.</param>
        public void Add(KeyModifier modifier, Keys key, char ch)
        {
            mapping[modifier][key] = ch;
        }

        /// <summary>
        /// Add a mapping to this key map, valid if no key modifier is active.
        /// </summary>
        /// <param name="key">the key to map.</param>
        /// <param name="ch">the char to map to.</param>
        public void Add(Keys key, char ch)
        {
            Add(KeyModifier.None, key, ch);
        }

        /// <summary>
        /// Get the char a key maps to.
        /// </summary>
        /// <param name="modifier">the active modifier.</param>
        /// <param name="key">the key to look up.</param>
        /// <returns>the applying char, or '\0' if there is no mapping for this combination.</returns>
        public char this[KeyModifier modifier, Keys key]
        {
            get
            {
                if (mapping[modifier].ContainsKey(key))
                {
                    return mapping[modifier][key];
                }
                else
                {
                    return '\0';
                }
            }
        }

        /// <summary>
        /// Get the char a key maps to, assuming no modifier is active.
        /// </summary>
        /// <param name="key">the key to look up.</param>
        /// <returns>the applying char, or '\0' if there is no mapping for this combination.</returns>
        public char this[Keys key]
        {
            get
            {
                return this[KeyModifier.None, key];
            }
        }

        #endregion

        #endregion

        #region Statics

        #region Initialization

        /// <summary>
        /// List of known locales.
        /// </summary>
        private static Dictionary<string, KeyMap> locales = new Dictionary<string, KeyMap>();

        /// <summary>
        /// Build up default key mappings.
        /// </summary>
        static KeyMap()
        {
            var enUS = BasicKeyMap;

            enUS.Add(KeyModifier.Shift, Keys.D0, ')');
            enUS.Add(KeyModifier.Shift, Keys.D1, '!');
            enUS.Add(KeyModifier.Shift, Keys.D2, '@');
            enUS.Add(KeyModifier.Shift, Keys.D3, '#');
            enUS.Add(KeyModifier.Shift, Keys.D4, '$');
            enUS.Add(KeyModifier.Shift, Keys.D5, '%');
            enUS.Add(KeyModifier.Shift, Keys.D6, '^');
            enUS.Add(KeyModifier.Shift, Keys.D7, '&');
            enUS.Add(KeyModifier.Shift, Keys.D8, '*');
            enUS.Add(KeyModifier.Shift, Keys.D9, '(');

            enUS.Add(Keys.Decimal, '.');

            enUS.Add(Keys.OemBackslash, '\\');
            enUS.Add(Keys.OemCloseBrackets, ']');
            enUS.Add(Keys.OemComma, ',');
            enUS.Add(Keys.OemMinus, '-');
            enUS.Add(Keys.OemOpenBrackets, '[');
            enUS.Add(Keys.OemPeriod, '.');
            enUS.Add(Keys.OemPipe, '\\');
            enUS.Add(Keys.OemPlus, '=');
            enUS.Add(Keys.OemQuestion, '/');
            enUS.Add(Keys.OemQuotes, '\'');
            enUS.Add(Keys.OemSemicolon, ';');
            enUS.Add(Keys.OemTilde, '`');

            enUS.Add(KeyModifier.Shift, Keys.OemBackslash, '|');
            enUS.Add(KeyModifier.Shift, Keys.OemCloseBrackets, '}');
            enUS.Add(KeyModifier.Shift, Keys.OemComma, '<');
            enUS.Add(KeyModifier.Shift, Keys.OemMinus, '_');
            enUS.Add(KeyModifier.Shift, Keys.OemOpenBrackets, '{');
            enUS.Add(KeyModifier.Shift, Keys.OemPeriod, '>');
            enUS.Add(KeyModifier.Shift, Keys.OemPipe, '|');
            enUS.Add(KeyModifier.Shift, Keys.OemPlus, '+');
            enUS.Add(KeyModifier.Shift, Keys.OemQuestion, '?');
            enUS.Add(KeyModifier.Shift, Keys.OemQuotes, '"');
            enUS.Add(KeyModifier.Shift, Keys.OemSemicolon, ':');
            enUS.Add(KeyModifier.Shift, Keys.OemTilde, '~');

            AddKeyMapForLocale("en-US", enUS);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the key map for the given locale. You can check the available locales
        /// via the <c>Locales</c> property.
        /// </summary>
        /// <param name="locale">the locale for which to get the key map.</param>
        /// <returns>the key map that may be used for this locale.</returns>
        public static KeyMap KeyMapByLocale(string locale)
        {
            return locales[locale];
        }

        /// <summary>
        /// Register a key map for a specific locale.
        /// </summary>
        /// <param name="locale">the locale for which the key map may be used.</param>
        /// <param name="keyMap">the key map that may be used for this locale.</param>
        public static void AddKeyMapForLocale(string locale, KeyMap keyMap)
        {
            locales[locale] = keyMap;
        }

        /// <summary>
        /// A list of all locales for which a key map is known.
        /// </summary>
        public static List<string> Locales
        {
            get
            {
                return new List<string>(locales.Keys);
            }
        }

        /// <summary>
        /// A very basic key map, including alphanumerics ([a-zA-Z0-9]), space,
        /// as well as numpad keys (numbers as well as mathematicl operators,
        /// excluding the <c>Keys.Decimal</c>, as this may be locale specific).
        /// </summary>
        public static KeyMap BasicKeyMap
        {
            get
            {
                KeyMap keyMap = new KeyMap();

                for (Keys key = Keys.A; key <= Keys.Z; ++key)
                {
                    keyMap.Add(key, (char)(key + 32));
                    keyMap.Add(KeyModifier.Shift, key, (char)key);
                }

                for (Keys key = Keys.D0; key <= Keys.D9; ++key)
                {
                    keyMap.Add(key, (char)key);
                }

                keyMap.Add(Keys.Space, ' ');

                keyMap.Add(Keys.NumPad0, '0');
                keyMap.Add(Keys.NumPad1, '1');
                keyMap.Add(Keys.NumPad2, '2');
                keyMap.Add(Keys.NumPad3, '3');
                keyMap.Add(Keys.NumPad4, '4');
                keyMap.Add(Keys.NumPad5, '5');
                keyMap.Add(Keys.NumPad6, '6');
                keyMap.Add(Keys.NumPad7, '7');
                keyMap.Add(Keys.NumPad8, '8');
                keyMap.Add(Keys.NumPad9, '9');

                keyMap.Add(Keys.Add, '+');
                keyMap.Add(Keys.Divide, '/');
                keyMap.Add(Keys.Multiply, '*');
                keyMap.Add(Keys.Separator, '|');
                keyMap.Add(Keys.Subtract, '-');

                return keyMap;
            }
        }

        #endregion

        #endregion

    }
}
