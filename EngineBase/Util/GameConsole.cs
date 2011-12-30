using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Engine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Keys = Microsoft.Xna.Framework.Input.Keys;


namespace Engine.Util
{

    /// <summary>
    /// This is a simple console which can easily be plugged into an XNA game.
    /// 
    /// It supports:
    /// <list type="bullet">
    /// <item>custom background / foreground color.</item>
    /// <item>custom font (not necessarily monospace).</item>
    /// <item>automatic line wrapping.</item>
    /// <item>scrolling through the buffer ([shift+]page up / [shift+]page down).</item>
    /// <item>command history (up / down).</item>
    /// <item>navigation in and manipulation of current input ([ctrl+]left / [ctrl+]right / home / end / delete / backspace).</item>
    /// <item>command completion (tab, only command names, not parameters).</item>
    /// <item>string literals ("").</item>
    /// </list>
    /// 
    /// It does not support:
    /// <list type="bullet">
    /// <item>animation on open / close.</item>
    /// <item>custom size / layout.</item>
    /// </list>
    /// </summary>
    /// <example>
    /// <code>
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
    /// </code>
    /// </example>
    public class GameConsole : DrawableGameComponent, IGameConsole
    {

        #region Constants

        /// <summary>
        /// Overall padding of the console.
        /// </summary>
        private const int Padding = 4;

        /// <summary>
        /// Regex used to parse parameters from input.
        /// </summary>
        private static readonly Regex ArgPattern = new Regex(@"
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

        /// <summary>
        /// Fired when an entry is added via WriteLine().
        /// </summary>
        public event EventHandler<EventArgs> LineWritten;

        #endregion

        #region Properties

        /// <summary>
        /// The texture used as the console background.
        /// </summary>
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// The maximum number of lines to keep.
        /// </summary>
        public int BufferSize
        {
            get
            {
                return _bufferSize;
            }
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

        /// <summary>
        /// The color of the caret (input position marker).
        /// </summary>
        public Color CaretColor { get; set; }

        /// <summary>
        /// The number of entries to skip when scrolling either via page up / down or the mouse wheel.
        /// </summary>
        public int EntriesToScroll { get; set; }

        /// <summary>
        /// The font to use for rendering text on the console.
        /// </summary>
        public SpriteFont Font { get; set; }

        /// <summary>
        /// The list of recent commands a user entered.
        /// </summary>
        public ICollection<string> History
        {
            get
            {
                return new List<string>(_history.ToArray());
            }
        }

        /// <summary>
        /// The hot-key used for opening the console.
        /// </summary>
        public Keys Hotkey { get; set; }

        /// <summary>
        /// Whether the console is currently open (visible) or not.
        /// </summary>
        public bool IsOpen { get; set; }

        /// <summary>
        /// The key map to use for resolving Xna key presses to chars.
        /// </summary>
        public KeyMap KeyMap { get; set; }

        /// <summary>
        /// SpriteBatch used for rendering.
        /// </summary>
        public SpriteBatch SpriteBatch { get; set; }

        /// <summary>
        /// Color to use for console text.
        /// </summary>
        public Color TextColor { get; set; }

        #endregion

        #region Fields

        /// <summary>
        /// Internal line buffer (lines of text).
        /// </summary>
        private List<string> _buffer = new List<string>();

        /// <summary>
        /// Actual value for the maximum number of lines to keep.
        /// </summary>
        private int _bufferSize = 200;

        /// <summary>
        /// List of known commands.
        /// </summary>
        private Dictionary<string, CommandInfo> _commands = new Dictionary<string, CommandInfo>();

        /// <summary>
        /// Input cursor offset.
        /// </summary>
        private int _cursor = 0;

        /// <summary>
        /// The history of commands a user entered.
        /// </summary>
        private List<string> _history = new List<string>();

        /// <summary>
        /// Which history index we last copied.
        /// </summary>
        private int _historyIndex = -1;

        /// <summary>
        /// Current user input.
        /// </summary>
        private StringBuilder _input = new StringBuilder();

        /// <summary>
        /// Backup of our last input, before cycling through the history.
        /// </summary>
        private string _inputBackup;

        /// <summary>
        /// Text we had before pressing tab the first time, to allow cycling through
        /// possible solutions.
        /// </summary>
        private string _inputBeforeTab;

        /// <summary>
        /// Last time a key was pressed (to suppress blinking for a bit while / after typing).
        /// </summary>
        private DateTime _lastKeyPress = DateTime.MinValue;

        /// <summary>
        /// Texture used for rendering the background.
        /// </summary>
        private Texture2D _pixelTexture;

        /// <summary>
        /// The current scrolling offset.
        /// </summary>
        private int _scroll;

        /// <summary>
        /// Index of last used tab completion option.
        /// </summary>
        private int _tabCompleteIndex = -1;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new game console and adds it as a service to the game.
        /// </summary>
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
            KeyMap = KeyMap.KeyMapByLocale("en-US");

            // Add inbuilt functions.
            AddCommand(new[] { "help", "?", "commands", "cmdlist" },
                HandleShowHelp,
                "Shows this help text.");
            AddCommand(new[] { "quit", "exit" },
                HandleExit,
                "Exits the program.");
            AddCommand(new[] { "clear", "cls" },
                HandleClear,
                "Clears the console screen.");

            // Register with game.
            game.Services.AddService(typeof(IGameConsole), this);

            // Draw on top of everything else.
            DrawOrder = int.MaxValue;
        }

        #endregion

        #region Init / Update / Cleanup

        public override void Initialize()
        {
            var keyboard = (IKeyboardInputManager)Game.Services.GetService(typeof(IKeyboardInputManager));
            if (keyboard != null)
            {
                keyboard.Pressed += HandleKeyPressed;

                keyboard.Combo(Keys.V, KeyModifier.Control).Pressed += HandleInsert;
            }
            var mouse = (IMouseInputManager)Game.Services.GetService(typeof(IMouseInputManager));
            if (mouse != null)
            {
                mouse.Scrolled += HandleMouseScrolled;
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _pixelTexture.SetData(new[] { Color.White });

            base.LoadContent();
        }

        protected override void Dispose(bool disposing)
        {
            var keyboard = (IKeyboardInputManager)Game.Services.GetService(typeof(IKeyboardInputManager));
            if (keyboard != null)
            {
                keyboard.Pressed -= HandleKeyPressed;
            }
            var mouse = (IMouseInputManager)Game.Services.GetService(typeof(IMouseInputManager));
            if (mouse != null)
            {
                mouse.Scrolled -= HandleMouseScrolled;
            }
            if (_pixelTexture != null)
            {
                _pixelTexture.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void Draw(GameTime gameTime)
        {
            if (IsOpen && BackgroundColor != null && SpriteBatch != null && Font != null)
            {
                Rectangle bounds = ComputeBounds();
                int numBufferLines = ComputeNumberOfVisibleLines() - 1;

                SpriteBatch.Begin();

                // Draw background.

                // Outer.
                SpriteBatch.Draw(_pixelTexture, bounds, new Color(64, 64, 64, BackgroundColor.A));
                SpriteBatch.Draw(_pixelTexture, bounds, BackgroundColor);

                // Content.
                SpriteBatch.Draw(_pixelTexture,
                    new Rectangle(bounds.X + Padding / 2, bounds.Y + Padding / 2,
                        bounds.Width - Padding, bounds.Height - Padding),
                    BackgroundColor);

                // Command line. We need to know the number of lines we have to properly render the background.
                List<String> inputWrapped = WrapText("> " + _input, bounds.Width - Padding * 2);

                SpriteBatch.Draw(_pixelTexture,
                    new Rectangle(bounds.X + Padding, bounds.Y + Padding + (numBufferLines - inputWrapped.Count + 1) * Font.LineSpacing,
                        bounds.Width - Padding * 2, Font.LineSpacing * inputWrapped.Count),
                    BackgroundColor);

                // Draw text. From bottom to top, for line wrapping.

                // Get rendering position.
                Vector2 position = new Vector2(bounds.X + Padding, bounds.Y + Padding + numBufferLines * Font.LineSpacing);

                // Draw the current command line.
                {
                    for (int i = inputWrapped.Count - 1; i >= 0 && numBufferLines >= 0; --i, --numBufferLines)
                    {
                        SpriteBatch.DrawString(Font, inputWrapped[i], position, TextColor);
                        position.Y -= Font.LineSpacing;
                    }

                    // Draw the cursor.
                    if (((int)gameTime.TotalGameTime.TotalSeconds & 1) == 0 || (new TimeSpan(DateTime.Now.Ticks - _lastKeyPress.Ticks).TotalSeconds < 1))
                    {
                        int cursorLine;
                        int cursorCounter = _cursor + 2;
                        for (cursorLine = 0; cursorLine < inputWrapped.Count - 1; ++cursorLine)
                        {
                            if (cursorCounter < inputWrapped[cursorLine].Length)
                            {
                                break;
                            }
                            cursorCounter -= inputWrapped[cursorLine].Length;
                        }
                        int cursorX = bounds.X + Padding + (int)Font.MeasureString(inputWrapped[cursorLine].Substring(0, cursorCounter)).X;
                        int cursorY = bounds.Y + Padding + (ComputeNumberOfVisibleLines() - (inputWrapped.Count - cursorLine)) * Font.LineSpacing;
                        int cursorWidth;
                        if (inputWrapped[cursorLine].Length > cursorCounter)
                        {
                            cursorWidth = (int)Font.MeasureString(inputWrapped[cursorLine][cursorCounter].ToString()).X;
                        }
                        else
                        {
                            cursorWidth = (int)Font.MeasureString(" ").X;
                        }

                        SpriteBatch.Draw(_pixelTexture, new Rectangle(cursorX, cursorY, cursorWidth, Font.LineSpacing), CaretColor);
                    }
                }

                // Draw text buffer.
                for (int j = _buffer.Count - 1 - _scroll; j >= 0 && numBufferLines >= 0; --j)
                {
                    List<String> wrapped = WrapText(_buffer[j], bounds.Width - Padding * 2);

                    for (int i = wrapped.Count - 1; i >= 0 && numBufferLines >= 0; --i, --numBufferLines)
                    {
                        SpriteBatch.DrawString(Font, wrapped[i], position, TextColor);
                        position.Y -= Font.LineSpacing;
                    }
                }

                SpriteBatch.End();
            }
            base.Draw(gameTime);
        }

        #endregion

        #region Public interface

        /// <summary>
        /// Register a new command with the given name.
        /// </summary>
        /// <param name="name">the name of the command.</param>
        /// <param name="handler">the function that will handle the command.</param>
        /// <param name="help">optional help that may be displayed for this command.</param>
        public void AddCommand(string name, CommandHandler handler, params string[] help)
        {
            if (!String.IsNullOrWhiteSpace(name))
            {
                AddCommand(new[] { name }, handler, help);
            }
            else
            {
                throw new ArgumentException("Invalid argument (the name, is empty or null).");
            }
        }

        /// <summary>
        /// Register a new command with aliases.
        /// </summary>
        /// <param name="names">command names  (first is considered the main name).</param>
        /// <param name="handler">the function that will handle the command.</param>
        /// <param name="help">optional help that may be displayed for this command.</param>
        public void AddCommand(string[] names, CommandHandler handler, params string[] help)
        {
            if (Array.TrueForAll(names, s => !String.IsNullOrWhiteSpace(s)) &&
                handler != null && (help == null || Array.TrueForAll(help, s => !String.IsNullOrWhiteSpace(s))))
            {
                var info = new CommandInfo((string[])names.Clone(), handler, (string[])help.Clone());
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
        /// Clears the complete buffer.
        /// </summary>
        public void Clear()
        {
            _scroll = 0;
            _buffer.Clear();
            _buffer.TrimExcess();
        }

        /// <summary>
        /// Execute a command in the format it would be written in the console, i.e. 'command arg0 arg1 ...'.
        /// </summary>
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
            var matches = ArgPattern.Matches(command);
            List<string> args = new List<string>();
            for (int i = 0; i < matches.Count; ++i)
            {
                string match = matches[i].Groups[1].Value;
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
                    _commands[args[0]].handler(args.ToArray());
                }
                catch (Exception e)
                {
#if DEBUG
                    WriteLine("Error: " + e.ToString());
#else
                    WriteLine("Error: " + e.Message);
#endif
                }
            }
            else
            {
                WriteLine("Error: unknown command '" + args[0] + "'. Try 'help' to see a list of available commands.");
            }
        }

        /// <summary>
        /// Log some text to the console.
        /// </summary>
        /// <param name="message">the message to log.</param>
        public void WriteLine(string message)
        {
            if (message == null)
            {
                return;
            }
            message = message.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] lines = message.Split('\n');
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

        /// <summary>
        /// Log some formatted text to the console.
        /// </summary>
        /// <param name="format">the text format.</param>
        /// <param name="args">the parameters to insert.</param>
        public void WriteLine(string format, params object[] args)
        {
            if (format == null)
            {
                return;
            }
            WriteLine(String.Format(CultureInfo.CurrentCulture, format, args));
        }

        #endregion

        #region Input

        /// <summary>
        /// Handle keyboard input.
        /// </summary>
        private void HandleKeyPressed(object sender, EventArgs e)
        {
            var args = (KeyboardInputEventArgs)e;
            if (IsOpen)
            {

                switch (args.Key)
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
                            if (_historyIndex == -1)
                            {
                                _input.Append(_inputBackup);
                            }
                            else
                            {
                                _input.Append(_history[_historyIndex]);
                            }
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
                    case Keys.Escape:
                        IsOpen = false;
                        ResetInput();
                        break;
                    case Keys.Home:
                        _cursor = 0;
                        ResetTabCompletion();
                        break;
                    case Keys.Left:
                        if (args.Modifier == KeyModifier.Control)
                        {
                            int startIndex = System.Math.Max(0, _cursor - 1);
                            while (startIndex > 0 && startIndex < _input.Length && _input[startIndex] == ' ')
                            {
                                --startIndex;
                            }
                            int index = _input.ToString().LastIndexOf(' ', startIndex);
                            if (index == -1)
                            {
                                _cursor = 0;
                            }
                            else
                            {
                                _cursor = System.Math.Min(_input.Length, index + 1);
                            }
                        }
                        else
                        {
                            _cursor = System.Math.Max(0, _cursor - 1);
                        }
                        ResetTabCompletion();
                        break;
                    case Keys.PageDown:
                        if (args.Modifier == KeyModifier.Shift)
                        {
                            _scroll = 0;
                        }
                        else
                        {
                            _scroll = System.Math.Max(0, _scroll - EntriesToScroll);
                        }
                        break;
                    case Keys.PageUp:
                        if (args.Modifier == KeyModifier.Shift)
                        {
                            _scroll = System.Math.Max(0, _buffer.Count - 1);
                        }
                        else
                        {
                            _scroll = System.Math.Max(0, System.Math.Min(_buffer.Count - 1, _scroll + EntriesToScroll));
                        }
                        break;
                    case Keys.Right:
                        if (args.Modifier == KeyModifier.Control)
                        {
                            int index = _input.ToString().IndexOf(' ', _cursor);
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
                            if (_inputBeforeTab == null)
                            {
                                _inputBeforeTab = _input.ToString().Substring(0, _cursor).Trim();
                            }
                            if (_inputBeforeTab.Length > 0)
                            {
                                int numMatches = -1;
                                bool testIfLast = false;
                                foreach (var command in _commands)
                                {
                                    if (command.Key.StartsWith(_inputBeforeTab, StringComparison.Ordinal))
                                    {
                                        ++numMatches;
                                        if (!testIfLast && _tabCompleteIndex < numMatches)
                                        {
                                            // Found a match we can use.
                                            _input.Clear();
                                            _input.Append(command.Key);
                                            _cursor = _input.Length;
                                            _tabCompleteIndex = numMatches;
                                            testIfLast = true;
                                        }
                                        else if (testIfLast)
                                        {
                                            testIfLast = false;
                                            break;
                                        }
                                    }
                                }
                                if (testIfLast)
                                {
                                    _tabCompleteIndex = -1;
                                }
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
                    default:
                        if (KeyMap != null)
                        {
                            char ch = KeyMap[args.Modifier, args.Key];
                            if (ch != '\0')
                            {
                                _input.Insert(_cursor, ch);
                                ++_cursor;
                                ResetTabCompletion();
                            }
                        }
                        break;
                }

                _lastKeyPress = DateTime.Now;
            }
            else
            {
                if (args.Key.Equals(Hotkey))
                {
                    IsOpen = true;
                }
            }
        }

        /// <summary>
        /// Handle request to paste from clipboard.
        /// </summary>
        private void HandleInsert(object sender, EventArgs e)
        {
            if (IsOpen && Clipboard.ContainsText())
            {
                string text = Clipboard.GetText().Replace("\n", "").Replace("\r", "");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _input.Append(text);
                    _cursor = _input.Length;
                }
            }
        }

        /// <summary>
        /// Handle mouse scrolling of the console buffer
        /// </summary>
        private void HandleMouseScrolled(object sender, EventArgs e)
        {
            if (IsOpen)
            {
                _scroll = System.Math.Max(0, System.Math.Min(_buffer.Count - 1, _scroll - System.Math.Sign(((MouseInputEventArgs)e).ScrollDelta) * EntriesToScroll));
            }
        }

        #endregion

        #region Utility methods

        private Rectangle ComputeBounds()
        {
            // Use top half of screen for the console. Cut off unused pixels due to line spacing.
            int height = GraphicsDevice.Viewport.Height / 2;
            if (Font != null)
            {
                height = height - (height % Font.LineSpacing);
            }
            height += 2 * Padding;
            return new Rectangle(GraphicsDevice.Viewport.X, GraphicsDevice.Viewport.Y,
                                 GraphicsDevice.Viewport.Width, height);
        }

        private int ComputeNumberOfVisibleLines()
        {
            return Font != null ? ((ComputeBounds().Height - Padding * 2) / Font.LineSpacing) : 0;
        }

        private List<string> WrapText(string text, int width)
        {
            List<string> result = new List<string>();
            do
            {
                // Use last value as initial guess.
                int split = FindSplit(text, 0, text.Length, true, width);
                result.Add(text.Substring(0, split));
                text = text.Substring(split);
            }
            while (!String.IsNullOrEmpty(text));
            return result;
        }

        private int FindSplit(string text, int low, int high, bool ceil, int width)
        {
            int mid = low + (high - low + (ceil ? 1 : 0)) / 2;
            if (mid == low)
            {
                return low;
            }
            int measure = (int)Font.MeasureString(text.Substring(0, mid)).X;
            if (measure <= width)
            {
                return FindSplit(text, mid, high, true, width);
            }
            else
            {
                return FindSplit(text, low, mid, false, width);
            }
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
            _tabCompleteIndex = -1;
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

            HashSet<string> shown = new HashSet<string>();

            foreach (var command in _commands.Values)
            {
                if (shown.Contains(command.names[0]))
                {
                    continue;
                }

                WriteLine(" " + command.names[0] + (command.names.Length > 1 ? (" [" + String.Join(", ", command.names, 1, command.names.Length - 1) + "]") : ""));
                if (command.help != null)
                {
                    foreach (var entry in command.help)
                    {
                        WriteLine("  " + entry);
                    }
                }

                foreach (var name in command.names)
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
    }

    #region Command helper

    /// <summary>
    /// Utility class that represents a single known command with all
    /// its aliases, handler and help text.
    /// </summary>
    class CommandInfo
    {
        /// <summary>
        /// All names for this command.
        /// </summary>
        public readonly string[] names;

        /// <summary>
        /// The handler method for this command.
        /// </summary>
        public readonly CommandHandler handler;

        /// <summary>
        /// Help text to display via the help command.
        /// </summary>
        public readonly string[] help;

        /// <summary>
        /// Creates a new helper object with the given values.
        /// </summary>
        public CommandInfo(string[] names, CommandHandler handler, string[] help)
        {
            this.names = names;
            this.handler = handler;
            this.help = help;
        }
    }

    #endregion

}
