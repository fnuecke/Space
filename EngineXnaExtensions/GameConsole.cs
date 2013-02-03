using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nuclex.Input;

namespace Engine.XnaExtensions
{
    /// <summary>
    ///     This is a simple console which can easily be plugged into an XNA game. It supports:
    ///     <list type="bullet">
    ///         <item>custom background / foreground color.</item>
    ///         <item>custom font (not necessarily mono-space).</item>
    ///         <item>automatic line wrapping.</item>
    ///         <item>scrolling through the buffer ([shift+]page up / [shift+]page down).</item>
    ///         <item>command history (up / down).</item>
    ///         <item>
    ///             navigation in and manipulation of current input ([ctrl+]left / [ctrl+]right / home / end / delete /
    ///             backspace).
    ///         </item>
    ///         <item>command completion (tab, only command names, not parameters).</item>
    ///         <item>string literals ("").</item>
    ///     </list>
    ///     It does not support:
    ///     <list type="bullet">
    ///         <item>animation on open / close.</item>
    ///         <item>custom size / layout.</item>
    ///     </list>
    /// </summary>
    /// <example>
    ///     <code>
    /// class MyGame : Game {
    /// 
    ///   GameConsole console;
    ///   
    ///   public MyGame() {
    ///     console = new GameConsole(this);
    ///   }
    /// 
    ///   protected override void LoadContent() {
    ///     // ...
    ///     console.SpriteBatch = spriteBatch;
    ///     console.Font = Content.Load&lt;SpriteFont&gt;("Fonts/ConsoleFont");
    ///   }
    ///     </code>
    /// </example>
    public sealed class GameConsole : DrawableGameComponent, IGameConsole
    {
        #region Constants

        /// <summary>Overall padding of the console.</summary>
        private const int Padding = 4;

        /// <summary>Regex used to parse parameters from input.</summary>
        private static readonly Regex ArgumentPattern = new Regex(@"
            \s*             # Leading whitespace.
            (               # Capture a string literal.
                ""              # String literal - open.
                (?:
                    \\.         # Either something escaped (e.g. ""s).
                    |           # or...
                    [^\\""]     # Anything not an escape start or a "".
                )*              # As often as there are escapes.
                ""              # String literal - close.
            |               # Or just text.
                (?:
                    [^\s]       # Anything that isn't whitespace.
                )*
            )", RegexOptions.IgnorePatternWhitespace);

        #endregion

        #region Events

        /// <summary>Fired when an entry is added via WriteLine().</summary>
        public event EventHandler<EventArgs> LineWritten;

        #endregion

        #region Properties

        /// <summary>The texture used as the console background.</summary>
        [PublicAPI]
        public Color BackgroundColor { get; set; }

        /// <summary>The maximum number of lines to keep.</summary>
        [PublicAPI]
        public int BufferSize
        {
            get { return _bufferSize; }
            set
            {
                _bufferSize = System.Math.Max(1, value);
                if (_buffer.Count > _bufferSize)
                {
                    _buffer.RemoveRange(BufferSize, _buffer.Count - BufferSize);
                }
                _buffer.TrimExcess();
            }
        }

        /// <summary>The color of the caret (input position marker).</summary>
        [PublicAPI]
        public Color CaretColor { get; set; }

        /// <summary>The number of entries to skip when scrolling either via page up / down or the mouse wheel.</summary>
        [PublicAPI]
        public int EntriesToScroll { get; set; }

        /// <summary>The font to use for rendering text on the console.</summary>
        [PublicAPI]
        public SpriteFont Font { get; set; }

        /// <summary>The list of recent commands a user entered.</summary>
        [PublicAPI]
        public ICollection<string> History
        {
            get { return new List<string>(_history.ToArray()); }
        }

        /// <summary>The hot-key used for opening the console.</summary>
        [PublicAPI]
        public Keys Hotkey { get; set; }

        /// <summary>Whether the console is currently open (visible) or not.</summary>
        [PublicAPI]
        public bool IsOpen
        {
            get { return _isOpen; }
            set
            {
                _shouldOpen = value;
                if (!value)
                {
                    _isOpen = false;
                }
            }
        }

        /// <summary>SpriteBatch used for rendering.</summary>
        [PublicAPI]
        public SpriteBatch SpriteBatch { get; set; }

        /// <summary>Color to use for console text.</summary>
        [PublicAPI]
        public Color TextColor { get; set; }

        #endregion

        #region Fields

        /// <summary>Internal line buffer (lines of text).</summary>
        private readonly List<string> _buffer = new List<string>();

        /// <summary>Actual value for the maximum number of lines to keep.</summary>
        private int _bufferSize = 200;

        /// <summary>List of known commands.</summary>
        private readonly Dictionary<string, CommandInfo> _commands = new Dictionary<string, CommandInfo>();

        /// <summary>List of additional callbacks to query for possible input.</summary>
        private readonly List<Func<IEnumerable<string>>> _additionalCommandNameGetters =
            new List<Func<IEnumerable<string>>>();

        /// <summary>Default command handler, used when an unknown command is encountered.</summary>
        private DefaultHandler _defaultHandler;

        /// <summary>Input cursor offset.</summary>
        private int _cursor;

        /// <summary>The history of commands a user entered.</summary>
        private readonly List<string> _history = new List<string>();

        /// <summary>Which history index we last copied.</summary>
        private int _historyIndex = -1;

        /// <summary>Current user input.</summary>
        private readonly StringBuilder _input = new StringBuilder();

        /// <summary>Backup of our last input, before cycling through the history.</summary>
        private string _inputBackup;

        /// <summary>Text we had before pressing tab the first time, to allow cycling through possible solutions.</summary>
        private string _inputBeforeTab;

        /// <summary>Last time a key was pressed (to suppress blinking for a bit while / after typing).</summary>
        private DateTime _lastKeyPress = DateTime.MinValue;

        /// <summary>Texture used for rendering the background.</summary>
        private Texture2D _pixelTexture;

        /// <summary>The current scrolling offset.</summary>
        private int _scroll;

        /// <summary>List of potential commands for tab completion.</summary>
        private List<string> _tabCompleteList;

        /// <summary>Index of last used tab completion option.</summary>
        private int _tabCompleteIndex;

        /// <summary>Whether to open the console in the next update. Used to skip the hot key from being printed in the console.</summary>
        private bool _shouldOpen;

        /// <summary>Whether the console actually is open at the moment.</summary>
        private bool _isOpen;

        /// <summary>Reused string buffer for lines to actually draw.</summary>
        private readonly List<StringBuilder> _lines = new List<StringBuilder>();

        /// <summary>Reused buffer for wrapping a single line of text.</summary>
        private readonly StringBuilder _wrap = new StringBuilder();

        /// <summary>Reused buffer for extracting substrings for measurement.</summary>
        private readonly StringBuilder _substring = new StringBuilder();

        #endregion

        #region Constructor

        /// <summary>Creates a new game console and adds it as a service to the game.</summary>
        /// <param name="game">the game the console will be used in.</param>
        public GameConsole(Game game)
            : base(game)
        {
            // Set defaults.
            BackgroundColor = new Color(0, 0, 0, 0.4f);
            EntriesToScroll = 3;
            TextColor = Color.WhiteSmoke;
            CaretColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
            Hotkey = Keys.OemTilde;

            // Add inbuilt functions.
            AddCommand(
                new[] {"help", "?", "commands", "cmdlist"},
                HandleShowHelp,
                "Shows this help text.");
            AddCommand(
                new[] {"quit", "exit"},
                HandleExit,
                "Exits the program.");
            AddCommand(
                new[] {"clear", "cls"},
                HandleClear,
                "Clears the console screen.");

            // Register with game.
            game.Services.AddService(typeof (IGameConsole), this);

            // Draw on top of everything else.
            DrawOrder = int.MaxValue;
        }

        #endregion

        #region Init / Update / Cleanup

        /// <summary>Initializes the console, attaching event listeners.</summary>
        public override void Initialize()
        {
            var inputManager = (InputManager) Game.Services.GetService(typeof (InputManager));
            foreach (var keyboard in inputManager.Keyboards)
            {
                if (keyboard.IsAttached)
                {
                    keyboard.KeyPressed += HandleKeyPressed;
                    keyboard.CharacterEntered += HandleCharacterEntered;
                }
            }

            foreach (var mouse in inputManager.Mice)
            {
                if (mouse.IsAttached)
                {
                    mouse.MouseWheelRotated += HandleMouseScrolled;
                }
            }

            base.Initialize();
        }

        /// <summary>Loads content for this console, generating the pixel texture.</summary>
        protected override void LoadContent()
        {
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _pixelTexture.SetData(new[] {Color.White});

            base.LoadContent();
        }

        /// <summary>Free any resources we hold and clean up event listeners.</summary>
        /// <param name="disposing">Whether we're currently disposing.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var inputManager = (InputManager) Game.Services.GetService(typeof (InputManager));
                foreach (var keyboard in inputManager.Keyboards)
                {
                    if (keyboard.IsAttached)
                    {
                        keyboard.KeyPressed -= HandleKeyPressed;
                        keyboard.CharacterEntered -= HandleCharacterEntered;
                    }
                }

                foreach (var mouse in inputManager.Mice)
                {
                    if (mouse.IsAttached)
                    {
                        mouse.MouseWheelRotated -= HandleMouseScrolled;
                    }
                }

                if (_pixelTexture != null)
                {
                    _pixelTexture.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>Checks if we should open the console.</summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_shouldOpen)
            {
                _isOpen = true;
                _shouldOpen = false;
            }
        }

        /// <summary>Draws the console to the screen.</summary>
        /// <param name="gameTime">Unused.</param>
        public override void Draw(GameTime gameTime)
        {
            if (IsOpen && SpriteBatch != null && Font != null)
            {
                var bounds = ComputeBounds();
                var lineCount = ComputeNumberOfVisibleLines() - 1;

                // Allocate string builders for individual lines.
                if (_lines.Count != lineCount)
                {
                    if (_lines.Count < lineCount)
                    {
                        for (var i = lineCount - _lines.Count; i > 0; --i)
                        {
                            _lines.Add(new StringBuilder());
                        }
                    }
                    else
                    {
                        for (var i = _lines.Count - lineCount; i > 0; --i)
                        {
                            _lines.RemoveAt(0);
                        }
                    }
                }

                SpriteBatch.Begin();

                // Draw background.

                // Outer.
                SpriteBatch.Draw(_pixelTexture, bounds, new Color(64, 64, 64, BackgroundColor.A));
                SpriteBatch.Draw(_pixelTexture, bounds, BackgroundColor);

                // Content.
                SpriteBatch.Draw(
                    _pixelTexture,
                    new Rectangle(
                        bounds.X + Padding / 2,
                        bounds.Y + Padding / 2,
                        bounds.Width - Padding,
                        bounds.Height - Padding),
                    BackgroundColor);

                _wrap.Clear();
                _wrap.Append("> ");
                _wrap.Append(_input);

                // Command line. We need to know the number of lines we have to properly render the background.
                var wrappedLines = WrapText(_wrap, bounds.Width - Padding * 2, _lines, lineCount);

                SpriteBatch.Draw(
                    _pixelTexture,
                    new Rectangle(
                        bounds.X + Padding,
                        bounds.Y + Padding + (lineCount - wrappedLines + 1) * Font.LineSpacing,
                        bounds.Width - Padding * 2,
                        Font.LineSpacing * wrappedLines),
                    BackgroundColor);

                // Draw text. From bottom to top, for line wrapping.

                // Get rendering position.
                var position = new Vector2(bounds.X + Padding, bounds.Y + Padding + lineCount * Font.LineSpacing);

                // Draw the current command line.
                {
                    for (var i = wrappedLines - 1; i >= 0 && lineCount >= 0; --i, --lineCount)
                    {
                        SpriteBatch.DrawString(Font, _lines[i], position, TextColor);
                        position.Y -= Font.LineSpacing;
                    }

                    // Draw the cursor.
                    if (((int) gameTime.TotalGameTime.TotalSeconds & 1) == 0 ||
                        (new TimeSpan(DateTime.UtcNow.Ticks - _lastKeyPress.Ticks).TotalSeconds < 1))
                    {
                        int cursorLine;
                        var cursorCounter = _cursor + 2;
                        for (cursorLine = 0; cursorLine < wrappedLines - 1; ++cursorLine)
                        {
                            if (cursorCounter < _lines[cursorLine].Length)
                            {
                                break;
                            }
                            cursorCounter -= _lines[cursorLine].Length;
                        }
                        _substring.Clear();
                        for (var i = 0; i < cursorCounter; i++)
                        {
                            _substring.Append(_lines[cursorLine][i]);
                        }
                        var cursorX = bounds.X + Padding + (int) Font.MeasureString(_substring).X;
                        var cursorY = bounds.Y + Padding +
                                      (ComputeNumberOfVisibleLines() - (wrappedLines - cursorLine)) * Font.LineSpacing;
                        int cursorWidth;
                        if (_lines[cursorLine].Length > cursorCounter)
                        {
                            cursorWidth =
                                (int)
                                Font.MeasureString(
                                    _lines[cursorLine][cursorCounter].ToString(CultureInfo.InvariantCulture)).X;
                        }
                        else
                        {
                            cursorWidth = (int) Font.MeasureString(" ").X;
                        }

                        SpriteBatch.Draw(
                            _pixelTexture, new Rectangle(cursorX, cursorY, cursorWidth, Font.LineSpacing), CaretColor);
                    }
                }

                // Draw text buffer.
                for (var j = _buffer.Count - 1 - _scroll; j >= 0 && lineCount >= 0; --j)
                {
                    _wrap.Clear();
                    _wrap.Append(_buffer[j]);
                    wrappedLines = WrapText(_wrap, bounds.Width - Padding * 2, _lines, lineCount);

                    for (var i = wrappedLines - 1; i >= 0 && lineCount >= 0; --i, --lineCount)
                    {
                        SpriteBatch.DrawString(Font, _lines[i], position, TextColor);
                        position.Y -= Font.LineSpacing;
                    }
                }

                SpriteBatch.End();
            }
            base.Draw(gameTime);
        }

        #endregion

        #region Public interface

        /// <summary>Register a new command with the given name.</summary>
        /// <param name="name">the name of the command.</param>
        /// <param name="handler">the function that will handle the command.</param>
        /// <param name="help">optional help that may be displayed for this command.</param>
        public void AddCommand(string name, CommandHandler handler, params string[] help)
        {
            if (!String.IsNullOrWhiteSpace(name))
            {
                AddCommand(new[] {name}, handler, help);
            }
            else
            {
                throw new ArgumentException("Invalid argument (the name, is empty or null).");
            }
        }

        /// <summary>Register a new command with aliases.</summary>
        /// <param name="names">command names  (first is considered the main name).</param>
        /// <param name="handler">the function that will handle the command.</param>
        /// <param name="help">optional help that may be displayed for this command.</param>
        public void AddCommand(string[] names, CommandHandler handler, params string[] help)
        {
            if (Array.TrueForAll(names, s => !String.IsNullOrWhiteSpace(s)) &&
                handler != null && (help == null || Array.TrueForAll(help, s => !String.IsNullOrWhiteSpace(s))))
            {
                var info = new CommandInfo(
                    (string[]) names.Clone(), handler, help == null ? null : (string[]) help.Clone());
                foreach (var command in names)
                {
                    // Remove old variant, if there is one.
                    _commands.Remove(command);
                    _commands.Add(command, info);
                }
            }
            else
            {
                throw new ArgumentException("Invalid argument (a name, help entry or the handler is empty or null).");
            }
        }

        /// <summary>
        ///     Sets a command handler to be invoked when an unknown command is executed. Pass <c>null</c> to unset the default
        ///     handler, back to the default.
        /// </summary>
        /// <param name="handler">The command handler to use for unknown commands.</param>
        public void SetDefaultCommandHandler(DefaultHandler handler)
        {
            _defaultHandler = handler;
        }

        /// <summary>
        ///     Registers a callback that can be used to query names for auto completion. This method will be called each time
        ///     we need to complete the input, i.e. the result is not cached.
        /// </summary>
        /// <remarks>
        ///     This is intended to allow providing auto completion hints for commands handled in a custom default command
        ///     handler.
        /// </remarks>
        /// <param name="getGlobalNames">Callback used to get available commands.</param>
        public void AddAutoCompletionLookup(Func<IEnumerable<string>> getGlobalNames)
        {
            _additionalCommandNameGetters.Add(getGlobalNames);
        }

        /// <summary>Clears the complete buffer.</summary>
        public void Clear()
        {
            _scroll = 0;
            _buffer.Clear();
            _buffer.TrimExcess();
        }

        /// <summary>Execute a command in the format it would be written in the console, i.e. 'command arg0 arg1 ...'.</summary>
        /// <param name="command">the command to execute.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void Execute(string command)
        {
            // Verify and cleanup input.
            if (String.IsNullOrWhiteSpace(command))
            {
                return;
            }
            command = command.Trim();

            // Push it to our history, output it and scroll down.
            _history.Remove(command);
            _history.Insert(0, command);
            WriteLine("> " + command);
            _scroll = 0;

            // Parse the input into separate strings, allowing for string literals.
            var matches = ArgumentPattern.Matches(command);
            var args = new List<string>();
            for (var i = 0; i < matches.Count; ++i)
            {
                var match = matches[i].Groups[1].Value;
                if (match.Length > 1 && match[0] == '"' && match[match.Length - 1] == '"')
                {
                    match = match.Substring(1, match.Length - 2);
                }
                if (match.Length > 0)
                {
                    args.Add(match);
                }
            }

            // Do we know that command?
            if (_commands.ContainsKey(args[0]))
            {
                try
                {
                    _commands[args[0]].Handler(args.ToArray());
                }
                catch (Exception e)
                {
#if DEBUG
                    WriteLine("Error: " + e);
#else
                    WriteLine("Error: " + e.Message);
#endif
                }
            }
            else
            {
                // If we have a default handler, use it, else print an error.
                if (_defaultHandler != null)
                {
                    _defaultHandler(command);
                }
                else
                {
                    WriteLine(
                        "Error: unknown command '" + args[0] +
                        "'. Try 'help' to see a list of available commands.");
                }
            }
        }

        /// <summary>Log some formatted text to the console.</summary>
        /// <param name="format">the text format.</param>
        /// <param name="args">the parameters to insert.</param>
        public void WriteLine(string format, params object[] args)
        {
            if (format == null)
            {
                return;
            }

            var message = new StringBuilder().AppendFormat(format, args);
            message.Replace("\r\n", "\n");
            message.Replace("\r", "\n");
            // Remove chars we cannot display.
            if (Font != null)
            {
                for (var i = message.Length - 1; i >= 0; --i)
                {
                    var c = message[i];
                    if (c != '\n' && c != '\t' && !Font.Characters.Contains(c))
                    {
                        message.Remove(i, 1);
                    }
                }
            }
            var lines = message.ToString().Split('\n');
            _buffer.AddRange(lines);
            if (_buffer.Count > BufferSize)
            {
                _buffer.RemoveRange(0, _buffer.Count - BufferSize);
            }

            foreach (var line in lines)
            {
                OnLineWritten(new LineWrittenEventArgs(line));
            }
        }

        #endregion

        #region Input

        /// <summary>Handle keyboard input.</summary>
        private void HandleKeyPressed(Keys key)
        {
            if (IsOpen)
            {
                switch (key)
                {
                    case Keys.Back:
                        if (_cursor > 0)
                        {
                            --_cursor;
                            _input.Remove(_cursor, 1);
                        }
                        ResetTabCompletion();
                        break;
                    case Keys.Delete:
                        if (_cursor < _input.Length)
                        {
                            _input.Remove(_cursor, 1);
                        }
                        ResetTabCompletion();
                        break;
                    case Keys.Down:
                        if (_historyIndex >= 0)
                        {
                            --_historyIndex;
                            _input.Clear();
                            _input.Append(_historyIndex == -1 ? _inputBackup : _history[_historyIndex]);
                            _cursor = _input.Length;
                            ResetTabCompletion();
                        }
                        break;
                    case Keys.End:
                        _cursor = _input.Length;
                        ResetTabCompletion();
                        break;
                    case Keys.Enter:
                        Execute(_input.ToString());
                        ResetInput();
                        break;
                    case Keys.Home:
                        _cursor = 0;
                        ResetTabCompletion();
                        break;
                    case Keys.Left:
                        if (IsControlPressed())
                        {
                            int startIndex = System.Math.Max(0, _cursor - 1);
                            while (startIndex > 0 && startIndex < _input.Length && _input[startIndex] == ' ')
                            {
                                --startIndex;
                            }
                            var index = _input.ToString().LastIndexOf(' ', startIndex);
                            _cursor = index == -1 ? 0 : System.Math.Min(_input.Length, index + 1);
                        }
                        else
                        {
                            _cursor = System.Math.Max(0, _cursor - 1);
                        }
                        ResetTabCompletion();
                        break;
                    case Keys.PageDown:
                        _scroll = IsShiftPressed() ? 0 : System.Math.Max(0, _scroll - EntriesToScroll);
                        break;
                    case Keys.PageUp:
                        _scroll = IsShiftPressed()
                                      ? System.Math.Max(0, _buffer.Count - 1)
                                      : System.Math.Max(0, System.Math.Min(_buffer.Count - 1, _scroll + EntriesToScroll));
                        break;
                    case Keys.Right:
                        if (IsControlPressed())
                        {
                            var index = _input.ToString().IndexOf(' ', _cursor);
                            if (index == -1)
                            {
                                _cursor = _input.Length;
                            }
                            else
                            {
                                _cursor = System.Math.Min(_input.Length, index + 1);
                                while (_cursor < _input.Length && _input[_cursor] == ' ')
                                {
                                    ++_cursor;
                                }
                            }
                        }
                        else
                        {
                            _cursor = System.Math.Min(_input.Length, _cursor + 1);
                        }
                        ResetTabCompletion();
                        break;
                    case Keys.Tab:
                        if (_cursor > 0)
                        {
                            if (_tabCompleteList == null)
                            {
                                // First time we're trying to complete,
                                // remember what our initial input was,
                                // i.e. the string we're trying to complete.
                                _inputBeforeTab = _input.ToString().Substring(0, _cursor).Trim();
                                // Build a list of all available terms.
                                if (_inputBeforeTab.Length > 0)
                                {
                                    _tabCompleteList =
                                        _commands.Keys.Union(_additionalCommandNameGetters.SelectMany(g => g()))
                                                 .Where(c => c.StartsWith(_inputBeforeTab, StringComparison.Ordinal))
                                                 .OrderBy(c => c).ToList();
                                }
                            }
                            else
                            {
                                if (IsShiftPressed())
                                {
                                    --_tabCompleteIndex;
                                }
                                else
                                {
                                    ++_tabCompleteIndex;
                                }
                            }
                            if (_tabCompleteList != null)
                            {
                                _tabCompleteIndex = (_tabCompleteIndex + _tabCompleteList.Count) % _tabCompleteList.Count;
                                _input.Clear();
                                _input.Append(_tabCompleteList[_tabCompleteIndex]);
                                _cursor = _input.Length;
                            }
                            else
                            {
                                ResetTabCompletion();
                            }
                        }
                        break;
                    case Keys.Up:
                        if (_historyIndex < _history.Count - 1)
                        {
                            if (_historyIndex == -1)
                            {
                                // Make backup.
                                _inputBackup = _input.ToString();
                            }
                            ++_historyIndex;
                            _input.Clear();
                            _input.Append(_history[_historyIndex]);
                            _cursor = _input.Length;
                            ResetTabCompletion();
                        }
                        break;
                    case Keys.C:
                        if (IsControlPressed() && _input.Length > 0)
                        {
                            // Copy current input buffer to clipboard.
                            //Clipboard.SetText(_input.ToString());
                            // Cancel current input.
                            ResetInput();
                        }
                        break;
                    case Keys.Insert:
                        if (IsShiftPressed() && Clipboard.ContainsText())
                        {
                            // Insert current clipboard into input buffer.
                            var text = new StringBuilder(Clipboard.GetText());

                            // Remove line breaks.
                            text.Replace("\n", "").Replace("\r", "");

                            // Remove chars we cannot display.
                            for (var i = text.Length - 1; i >= 0; --i)
                            {
                                if (!Font.Characters.Contains(text[i]))
                                {
                                    text.Remove(i, 1);
                                }
                            }
                            var value = text.ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                _input.Insert(_cursor, value);
                                _cursor += value.Length;
                                ResetTabCompletion();
                            }
                        }
                        break;
                    default:
                        if (key == Hotkey)
                        {
                            IsOpen = false;
                            ResetInput();
                        }
                        break;
                }

                _lastKeyPress = DateTime.UtcNow;
            }
            else
            {
                if (key.Equals(Hotkey))
                {
                    _shouldOpen = true;
                }
            }
        }

        private void HandleCharacterEntered(char ch)
        {
            // Ignore input when not open, if it's a control char, or we cannot display the char.
            if (!IsOpen || char.IsControl(ch) || !Font.Characters.Contains(ch))
            {
                return;
            }

            // Don't take ctrl+c (cancels input).
            if ((ch == 'c') && IsControlPressed())
            {
                return;
            }

            // Else insert the char into our input.
            _input.Insert(_cursor, ch);
            ++_cursor;
            ResetTabCompletion();
        }

        /// <summary>Handle mouse scrolling of the console buffer</summary>
        private void HandleMouseScrolled(float ticks)
        {
            if (IsOpen)
            {
                _scroll = System.Math.Max(0, System.Math.Min(_buffer.Count - 1, _scroll + System.Math.Sign(ticks) * EntriesToScroll));
            }
        }

        #endregion

        #region Utility methods

        private bool IsControlPressed()
        {
            var inputManager = (InputManager) Game.Services.GetService(typeof (InputManager));
            return (from keyboard in inputManager.Keyboards
                    where keyboard.IsAttached
                    select keyboard.GetState()).Any(
                        state => state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl));
        }

        private bool IsShiftPressed()
        {
            var inputManager = (InputManager) Game.Services.GetService(typeof (InputManager));
            return (from keyboard in inputManager.Keyboards
                    where keyboard.IsAttached
                    select keyboard.GetState()).Any(
                        state => state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift));
        }

        private Rectangle ComputeBounds()
        {
            // Use top half of screen for the console. Cut off unused pixels due to line spacing.
            var height = GraphicsDevice.Viewport.Height / 2;
            if (Font != null)
            {
                height = height - (height % Font.LineSpacing);
            }
            height += 2 * Padding;
            return new Rectangle(
                GraphicsDevice.Viewport.X,
                GraphicsDevice.Viewport.Y,
                GraphicsDevice.Viewport.Width,
                height);
        }

        private int ComputeNumberOfVisibleLines()
        {
            return Font != null ? ((ComputeBounds().Height - Padding * 2) / Font.LineSpacing) : 0;
        }

        private int WrapText(StringBuilder text, int width, IList<StringBuilder> lines, int availableLines)
        {
            var i = 0;
            var position = 0;
            while (position < text.Length && i < availableLines)
            {
                // Use last value as initial guess.
                var split = FindSplit(text, position, position, text.Length, true, width);
                lines[i].Clear();
                for (var j = position; j < split; j++)
                {
                    lines[i].Append(text[j]);
                }
                position = split;
                ++i;
            }
            return i;
        }

        private int FindSplit(StringBuilder text, int start, int low, int high, bool ceiling, int width)
        {
            var mid = low + (high - low + (ceiling ? 1 : 0)) / 2;
            if (mid == low)
            {
                return low;
            }
            _substring.Clear();
            for (var i = start; i < mid; i++)
            {
                _substring.Append(text[i]);
            }
            var measure = (int) Font.MeasureString(_substring).X;
            return measure <= width
                       ? FindSplit(text, start, mid, high, true, width)
                       : FindSplit(text, start, low, mid, false, width);
        }

        private void ResetInput()
        {
            _inputBackup = null;
            _historyIndex = -1;
            _cursor = 0;
            _input.Clear();
            ResetTabCompletion();
        }

        private void ResetTabCompletion()
        {
            _inputBeforeTab = null;
            _tabCompleteList = null;
            _tabCompleteIndex = 0;
        }

        private void OnLineWritten(LineWrittenEventArgs e)
        {
            if (LineWritten != null)
            {
                LineWritten(this, e);
            }
        }

        #endregion

        #region Inbuilt commands

        private void HandleShowHelp(string[] args)
        {
            WriteLine("Known commands:");

            var shown = new HashSet<string>();

            foreach (var command in _commands.Values)
            {
                if (shown.Contains(command.Names[0]))
                {
                    continue;
                }

                WriteLine(
                    " " + command.Names[0] +
                    (command.Names.Length > 1
                         ? (" [" + String.Join(", ", command.Names, 1, command.Names.Length - 1) + "]")
                         : ""));
                if (command.Help != null)
                {
                    foreach (var entry in command.Help)
                    {
                        WriteLine("  " + entry);
                    }
                }

                foreach (var name in command.Names)
                {
                    shown.Add(name);
                }
            }
        }

        private void HandleExit(string[] args)
        {
            Game.Exit();
        }

        private void HandleClear(string[] args)
        {
            Clear();
        }

        #endregion

        #region Command helper

        /// <summary>Utility class that represents a single known command with all its aliases, handler and help text.</summary>
        private sealed class CommandInfo
        {
            /// <summary>All names for this command.</summary>
            public readonly string[] Names;

            /// <summary>The handler method for this command.</summary>
            public readonly CommandHandler Handler;

            /// <summary>Help text to display via the help command.</summary>
            public readonly string[] Help;

            /// <summary>Creates a new helper object with the given values.</summary>
            public CommandInfo(string[] names, CommandHandler handler, string[] help)
            {
                Names = names;
                Handler = handler;
                Help = help;
            }
        }

        #endregion
    }
}