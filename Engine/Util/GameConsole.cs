using System;
using System.Collections.Generic;
using System.Text;
using Engine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Engine.Util
{

    public delegate void CommandHandler(string[] args);
    public delegate void LineWrittenEventHandler(string line);

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
    /// </list>
    /// 
    /// It does not support:
    /// <list type="bullet">
    /// <item>animation on open / close.</item>
    /// <item>custom size.</item>
    /// </list>
    /// 
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
    public class GameConsole : DrawableGameComponent
    {

        #region Constants

        /// <summary>
        /// Overall padding of the console.
        /// </summary>
        private const int Padding = 4;

        #endregion

        #region Events

        /// <summary>
        /// Fired when an entry is added via WriteLine().
        /// </summary>
        public event LineWrittenEventHandler LineWritten;

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
                return bufferSize;
            }
            set
            {
                bufferSize = System.Math.Max(1, value);
                if (buffer.Count > bufferSize)
                {
                    buffer.RemoveRange(BufferSize, buffer.Count - BufferSize);
                }
                buffer.TrimExcess();
            }
        }

        /// <summary>
        /// The color of the caret (input position marker).
        /// </summary>
        public Color CaretColor { get; set; }

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
                return new List<string>(history.ToArray());
            }
        }

        /// <summary>
        /// The hot-key used for toggling the console.
        /// </summary>
        public Keys HotKey { get; set; }

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
        private List<string> buffer = new List<string>();

        /// <summary>
        /// Actual value for the maximum number of lines to keep.
        /// </summary>
        private int bufferSize = 200;

        /// <summary>
        /// List of known commands.
        /// </summary>
        private Dictionary<string, CommandInfo> commands = new Dictionary<string, CommandInfo>();

        /// <summary>
        /// Input cursor offset.
        /// </summary>
        private int cursor = 0;

        /// <summary>
        /// The history of commands a user entered.
        /// </summary>
        private List<string> history = new List<string>();

        /// <summary>
        /// Which history index we last copied.
        /// </summary>
        private int historyIndex = -1;

        /// <summary>
        /// Current user input.
        /// </summary>
        private StringBuilder input = new StringBuilder();

        /// <summary>
        /// Backup of our last input, before cycling through the history.
        /// </summary>
        private string inputBackup;

        /// <summary>
        /// Text we had before pressing tab the first time, to allow cycling through
        /// possible solutions.
        /// </summary>
        private string inputBeforeTab;

        /// <summary>
        /// Last time a key was pressed (to suppress blinking for a bit while / after typing).
        /// </summary>
        private DateTime lastKeyPress = DateTime.MinValue;

        /// <summary>
        /// Texture used for rendering the background.
        /// </summary>
        private Texture2D pixelTexture;

        /// <summary>
        /// The current scrolling offset.
        /// </summary>
        private int scroll;

        /// <summary>
        /// Index of last used tab completion option.
        /// </summary>
        private int tabCompleteIndex = -1;

        #endregion

        /// <summary>
        /// Creates a new game console.
        /// </summary>
        /// <param name="game">the game the console will be used in.</param>
        public GameConsole(Game game)
            : base(game)
        {
            // Set defaults.
            BackgroundColor = new Color(0, 0, 0, 0.4f);
            TextColor = Color.WhiteSmoke;
            CaretColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
            HotKey = Keys.OemTilde;
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
            game.Components.Add(this);
            game.Services.AddService(typeof(GameConsole), this);
        }

        #region Init / Update

        public override void Initialize()
        {
            KeyboardInputManager input = (KeyboardInputManager)Game.Services.GetService(typeof(KeyboardInputManager));
            input.Pressed += HandleKeyPressed;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            pixelTexture.SetData(new[] { Color.White });

            base.LoadContent();
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
                SpriteBatch.Draw(pixelTexture, bounds, BackgroundColor);

                // Content.
                SpriteBatch.Draw(pixelTexture,
                    new Rectangle(bounds.X + Padding / 2, bounds.Y + Padding / 2,
                        bounds.Width - Padding, bounds.Height - Padding),
                    BackgroundColor);

                // Command line. We need to know the number of lines we have to properly render the background.
                List<String> inputWrapped = WrapText("> " + input, bounds.Width - Padding * 2);

                SpriteBatch.Draw(pixelTexture,
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
                    if (((int)gameTime.TotalGameTime.TotalSeconds & 1) == 0 || (lastKeyPress != null && new TimeSpan(DateTime.Now.Ticks - lastKeyPress.Ticks).TotalSeconds < 1))
                    {
                        int cursorLine;
                        int cursorCounter = cursor + 2;
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

                        SpriteBatch.Draw(pixelTexture, new Rectangle(cursorX, cursorY, cursorWidth, Font.LineSpacing), CaretColor);
                    }
                }

                // Draw text buffer.
                for (int j = buffer.Count - 1 - scroll; j >= 0 && numBufferLines >= 0; --j)
                {
                    List<String> wrapped = WrapText(buffer[j], bounds.Width - Padding * 2);

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
                    commands.Add(command, info);
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
            scroll = 0;
            buffer.Clear();
            buffer.TrimExcess();
        }

        /// <summary>
        /// Execute a command in the format it would be written in the console, i.e. 'command arg0 arg1 ...'.
        /// </summary>
        /// <param name="command">the command to execute.</param>
        public void Execute(string command)
        {
            if (String.IsNullOrWhiteSpace(command))
            {
                return;
            }
            command = command.Trim();

            history.Remove(command);
            history.Insert(0, command);
            WriteLine("> " + input.ToString());
            scroll = 0;

            string[] parts = command.Split(new[] { ' ' }, 2);

            if (commands.ContainsKey(parts[0]))
            {
                try
                {
                    if (parts.Length > 1)
                    {
                        commands[parts[0]].handler(parts[1].Split(' '));
                    }
                    else
                    {
                        commands[parts[0]].handler(new string[0]);
                    }
                }
                catch (Exception e)
                {
                    WriteLine("Error: " + e.ToString());
                }
            }
            else
            {
                WriteLine("Error: unknown command '" + parts[0] + "'. Try 'help' to see a list of available commands.");
            }
        }

        /// <summary>
        /// Log some text to the console.
        /// </summary>
        /// <param name="message">the message to log.</param>
        public void WriteLine(string message)
        {
            message = message.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] lines = message.Split('\n');
            buffer.AddRange(lines);
            if (buffer.Count > BufferSize)
            {
                buffer.RemoveRange(BufferSize, buffer.Count - BufferSize);
            }

            foreach (var line in lines)
            {
                OnLineWritten(line);
            }
        }

        #endregion

        #region Input

        /// <summary>
        /// Handle keyboard input.
        /// </summary>
        private void HandleKeyPressed(Keys key, KeyModifier modifier)
        {
            if (IsOpen)
            {
                switch (key)
                {
                    case Keys.Back:
                        if (cursor > 0)
                        {
                            --cursor;
                            input.Remove(cursor, 1);
                        }
                        ResetTabCompletion();
                        break;
                    case Keys.Delete:
                        if (cursor < input.Length)
                        {
                            input.Remove(cursor, 1);
                        }
                        ResetTabCompletion();
                        break;
                    case Keys.Down:
                        if (historyIndex >= 0)
                        {
                            --historyIndex;
                            cursor = 0;
                            input.Clear();
                            if (historyIndex == -1)
                            {
                                input.Append(inputBackup);
                            }
                            else
                            {
                                input.Append(history[historyIndex]);
                            }
                            ResetTabCompletion();
                        }
                        break;
                    case Keys.End:
                        cursor = input.Length;
                        ResetTabCompletion();
                        break;
                    case Keys.Enter:
                        Execute(input.ToString());
                        ResetInput();
                        break;
                    case Keys.Escape:
                        IsOpen = false;
                        ResetInput();
                        break;
                    case Keys.Home:
                        cursor = 0;
                        ResetTabCompletion();
                        break;
                    case Keys.Left:
                        if (modifier == KeyModifier.Control)
                        {
                            int startIndex = System.Math.Max(0, cursor - 1);
                            while (startIndex > 0 && startIndex < input.Length && input[startIndex] == ' ')
	                        {
                                --startIndex;
	                        }
                            int index = input.ToString().LastIndexOf(' ', startIndex);
                            if (index == -1)
                            {
                                cursor = 0;
                            }
                            else
                            {
                                cursor = System.Math.Min(input.Length, index + 1);
                            }
                        }
                        else
                        {
                            cursor = System.Math.Max(0, cursor - 1);
                        }
                        ResetTabCompletion();
                        break;
                    case Keys.PageDown:
                        if (modifier == KeyModifier.Shift)
                        {
                            scroll = 0;
                        }
                        else
                        {
                            scroll = System.Math.Max(0, scroll - 1);
                        }
                        break;
                    case Keys.PageUp:
                        if (modifier == KeyModifier.Shift)
                        {
                            scroll = System.Math.Max(0, buffer.Count - 1);
                        }
                        else
                        {
                            scroll = System.Math.Max(0, System.Math.Min(buffer.Count - 1, scroll + 1));
                        }
                        break;
                    case Keys.Right:
                        if (modifier == KeyModifier.Control)
                        {
                            int index = input.ToString().IndexOf(' ', cursor);
                            if (index == -1)
                            {
                                cursor = input.Length;
                            }
                            else
                            {
                                cursor = System.Math.Min(input.Length, index + 1);
                                while (cursor < input.Length && input[cursor] == ' ')
                                {
                                    ++cursor;
                                }
                            }
                        }
                        else
                        {
                            cursor = System.Math.Min(input.Length, cursor + 1);
                        }
                        ResetTabCompletion();
                        break;
                    case Keys.Tab:
                        if (cursor > 0)
                        {
                            if (inputBeforeTab == null)
                            {
                                inputBeforeTab = input.ToString().Substring(0, cursor).Trim();
                            }
                            if (inputBeforeTab.Length > 0)
                            {
                                int numMatches = -1;
                                bool testIfLast = false;
                                foreach (var command in commands)
                                {
                                    if (command.Key.StartsWith(inputBeforeTab))
                                    {
                                        ++numMatches;
                                        if (!testIfLast && tabCompleteIndex < numMatches)
                                        {
                                            // Found a match we can use.
                                            input.Clear();
                                            input.Append(command.Key);
                                            cursor = input.Length;
                                            tabCompleteIndex = numMatches;
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
                                    tabCompleteIndex = -1;
                                }
                            }
                            else
                            {
                                ResetTabCompletion();
                            }
                        }
                        break;
                    case Keys.Up:
                        if (historyIndex < history.Count - 1)
                        {
                            if (historyIndex == -1)
                            {
                                // Make backup.
                                inputBackup = input.ToString();
                            }
                            ++historyIndex;
                            cursor = 0;
                            input.Clear();
                            input.Append(history[historyIndex]);
                            ResetTabCompletion();
                        }
                        break;
                    default:
                        if (KeyMap != null)
                        {
                            char ch = KeyMap[modifier, key];
                            if (ch != '\0')
                            {
                                input.Insert(cursor, ch);
                                ++cursor;
                                ResetTabCompletion();
                            }
                        }
                        break;
                }

                lastKeyPress = DateTime.Now;
            }
            else
            {
                if (key.Equals(HotKey))
                {
                    IsOpen = true;
                }
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
            inputBackup = null;
            historyIndex = -1;
            cursor = 0;
            input.Clear();
            ResetTabCompletion();
        }

        private void ResetTabCompletion()
        {
            inputBeforeTab = null;
            tabCompleteIndex = -1;
        }

        private void OnLineWritten(string line)
        {
            if (LineWritten != null)
            {
                LineWritten(line);
            }
        }

        #endregion

        #region Inbuilt commands

        private void HandleShowHelp(string[] args)
        {
            WriteLine("Known commands:");

            HashSet<string> shown = new HashSet<string>();

            foreach (var command in commands.Values)
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

    class CommandInfo
    {
        public readonly string[] names;
        public readonly CommandHandler handler;
        public readonly string[] help;

        public CommandInfo(string[] names, CommandHandler handler, string[] help)
        {
            this.names = names;
            this.handler = handler;
            this.help = help;
        }
    }
}
