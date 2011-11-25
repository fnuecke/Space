namespace Engine.Util
{

    #region Delegates

    /// <summary>
    /// Signature for command handler functions.
    /// </summary>
    /// <param name="args">the arguments for the command (space separated strings).</param>
    public delegate void CommandHandler(string[] args);

    /// <summary>
    /// Signature for event handlers called when a line is written to the console.
    /// </summary>
    /// <param name="line">the text on the line that was written.</param>
    public delegate void LineWrittenEventHandler(string line);

    #endregion

    /// <summary>
    /// This is a simple console which can easily be plugged into an XNA game.
    /// 
    /// <para>
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
    /// </para>
    /// 
    /// <para>
    /// It does not support:
    /// <list type="bullet">
    /// <item>animation on open / close.</item>
    /// <item>custom size / layout.</item>
    /// </list>
    /// </para>
    /// </summary>
    public interface IGameConsole
    {

        /// <summary>
        /// Fired when an entry is added via WriteLine().
        /// </summary>
        event LineWrittenEventHandler LineWritten;

        /// <summary>
        /// Register a new command with the given name.
        /// </summary>
        /// <param name="name">the name of the command.</param>
        /// <param name="handler">the function that will handle the command.</param>
        /// <param name="help">optional help that may be displayed for this command.</param>
        void AddCommand(string name, CommandHandler handler, params string[] help);

        /// <summary>
        /// Register a new command with aliases.
        /// </summary>
        /// <param name="names">command names  (first is considered the main name).</param>
        /// <param name="handler">the function that will handle the command.</param>
        /// <param name="help">optional help that may be displayed for this command.</param>
        void AddCommand(string[] names, CommandHandler handler, params string[] help);

        /// <summary>
        /// Clears the complete buffer.
        /// </summary>
        void Clear();

        /// <summary>
        /// Execute a command in the format it would be written in the console, i.e. 'command arg0 arg1 ...'.
        /// </summary>
        /// <param name="command">the command to execute.</param>
        void Execute(string command);

        /// <summary>
        /// Log some text to the console.
        /// </summary>
        /// <param name="message">the message to log.</param>
        void WriteLine(string message);

    }
}
