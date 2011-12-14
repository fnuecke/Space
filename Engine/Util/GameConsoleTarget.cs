using System;
using Microsoft.Xna.Framework;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Engine.Util
{
    /// <summary>
    /// Target to log to a GameConsole instance attached to a given game.
    /// </summary>
    [Target("GameConsole")]
    public sealed class GameConsoleTarget : TargetWithLayout
    {
        /// <summary>
        /// The actual console to log to.
        /// </summary>
        private IGameConsole _console;

        /// <summary>
        /// Creates a new target, logging to the console of the specified game,
        /// at the specified level.
        /// </summary>
        /// <param name="game">the game to create the console for.</param>
        /// <param name="level">the log level starting at which to output messages.</param>
        public GameConsoleTarget(Game game, LogLevel level)
        {
            _console = (IGameConsole)game.Services.GetService(typeof(IGameConsole));

            if (_console == null)
            {
                throw new ArgumentException("Game does not have an IGameConsole service.", "game");
            }

            // Set a nicer layout.
            Layout = new SimpleLayout("${date:format=yyyy-MM-dd HH\\:mm\\:ss} [${level:uppercase=true}] ${logger:shortName=true}: ${message}");

            // Add as a target, or make it the default target if there are none.
            if (LogManager.Configuration != null)
            {
                LogManager.Configuration.AddTarget("GameConsole", this);
                LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", level, this));
                LogManager.Configuration.Reload();
            }
            else
            {
                SimpleConfigurator.ConfigureForTargetLogging(this, level);
            }
        }

        /// <summary>
        /// Do actual logging by writing to console.
        /// </summary>
        /// <param name="logEvent">the log event to process.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            _console.WriteLine(this.Layout.Render(logEvent));
        }
    }
}
