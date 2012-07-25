using System;
using System.Collections.Generic;

namespace Engine.Util
{
    #region Delegates

    /// <summary>
    /// Signature for command handler functions.
    /// </summary>
    /// <param name="args">The arguments for the command (space separated
    /// strings), where the first one is the name of the command itself.</param>
    public delegate void CommandHandler(string[] args);

    /// <summary>
    /// Signature for default command handler function.
    /// </summary>
    /// <param name="command">The command that should have been executed, but failed.</param>
    public delegate void DefaultHandler(string command);

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
    public interface IGameConsole : IDisposable
    {
        /// <summary>
        /// Fired when an entry is added via WriteLine(). Event args are of type
        /// <see cref="LineWrittenEventArgs"/>.
        /// </summary>
        event EventHandler<EventArgs> LineWritten;

        /// <summary>
        /// Whether the console is currently open (visible) or not.
        /// </summary>
        bool IsOpen { get; set; }

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
        /// Sets a command handler to be invoked when an unknown command is
        /// executed. Pass <c>null</c> to unset the default handler, back to
        /// the default.
        /// </summary>
        /// <param name="handler">The command handler to use for unknown
        /// commands.</param>
        void SetDefaultCommandHandler(DefaultHandler handler);

        /// <summary>
        /// Registers a callback that can be used to query names for
        /// auto completion. This method will be called each time we need
        /// to complete the input, i.e. the result is not cached.
        /// </summary>
        /// <remarks>
        /// This is intended to allow providing auto completion hints
        /// for commands handled in a custom default command handler.
        /// </remarks>
        /// <param name="getGlobalNames">Callback used to get available commands.</param>
        void AddAutoCompletionLookup(Func<IEnumerable<string>> getGlobalNames);

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
        /// Log some formatted text to the console.
        /// </summary>
        /// <param name="format">the text format.</param>
        /// <param name="args">the parameters to insert.</param>
        void WriteLine(string format, params object[] args);
    }
}
