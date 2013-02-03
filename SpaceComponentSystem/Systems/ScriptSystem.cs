using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using Engine.ComponentSystem.Systems;
using Engine.Serialization;
using Engine.Util;
using IronPython.Hosting;
using JetBrains.Annotations;
using Microsoft.Scripting.Hosting;

namespace Space.ComponentSystem.Systems
{
    /// <summary>Provides scripting facilities using IronPython.</summary>
    [Packetizable(false)]
    public class ScriptSystem : AbstractSystem
    {
        #region Logger

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #endregion
        
        #region Type ID

        /// <summary>The unique type ID for this system, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        #endregion

        #region Properties

        /// <summary>Gets the global names currently registered in the scripting environment.</summary>
        [PublicAPI]
        public IEnumerable<string> GlobalNames
        {
            get
            {
                var globals = _script.Runtime.Globals.GetVariableNames();
                var builtin = _script.Runtime.GetBuiltinModule().GetVariableNames()
                                     // Skip private and magic stuff.
                                     .Where(name => !name.StartsWith("__"));
                return globals.Union(builtin);
            }
        }

        #endregion

        #region Scripting environment

        /// <summary>The script used for initializing the environment, reused when copying.</summary>
        private readonly string _initScript;

        /// <summary>The global scripting engine we'll be using.</summary>
        [CopyIgnore]
        private ScriptEngine _script;
        
        /// <summary>The stream writer pushing info messages to the logger.</summary>
        [CopyIgnore]
        private InfoStreamWriter _infoStreamWriter;

        /// <summary>The stream writer pushing error messages to the logger.</summary>
        [CopyIgnore]
        private ErrorStreamWriter _errorStreamWriter;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="ScriptSystem"/> class.
        /// </summary>
        /// <param name="initScript">A script that is run immediately, to initialize the scripting environment.</param>
        public ScriptSystem(string initScript)
        {
            _initScript = initScript;
            Initialize();
        }

        private void Initialize()
        {
            // Create new engine.
            _script = Python.CreateEngine();

            // Load the executing and all referenced assemblies into the script environment.
            var executingAssembly = Assembly.GetEntryAssembly();
            _script.Runtime.LoadAssembly(executingAssembly);
            foreach (var assembly in executingAssembly.GetReferencedAssemblies())
            {
                _script.Runtime.LoadAssembly(Assembly.Load(assembly));
            }

            // Redirect scripting output to the logger.
            var infoStream = new System.IO.MemoryStream();
            var errorStream = new System.IO.MemoryStream();
            _infoStreamWriter = new InfoStreamWriter(infoStream);
            _errorStreamWriter = new ErrorStreamWriter(errorStream);
            _script.Runtime.IO.SetOutput(infoStream, _infoStreamWriter);
            _script.Runtime.IO.SetErrorOutput(errorStream, _errorStreamWriter);

            // Register some macros in our scripting environment.
            try
            {
                Execute(_initScript);
            }
            catch (InvalidOperationException ex)
            {
                Logger.WarnException("Failed initializing script engine, faulty init script.", ex);
            }
        }

        public override void OnAddedToManager()
        {
            base.OnAddedToManager();

            _script.Runtime.Globals.SetVariable("manager", Manager);
        }

        /// <summary>Executes the specified script.</summary>
        /// <param name="script">The script to execute.</param>
        /// <returns>The result of the script execution.</returns>
        [PublicAPI]
        public dynamic Execute(string script)
        {
            try
            {
                return _script.Execute(script, _script.Runtime.Globals);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(_script.GetService<ExceptionOperations>().FormatException(ex), ex);
            }
        }

        /// <summary>Calls a function of the specified, passing it the specified parameters.</summary>
        /// <param name="function">The name of the function to call.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>The return value of the function.</returns>
        [PublicAPI]
        public dynamic Call(string function, params object[] args)
        {
            try
            {
                ObjectHandle handle;
                if (!_script.Runtime.Globals.TryGetVariableHandle(function, out handle) &&
                    !_script.Runtime.GetBuiltinModule().TryGetVariableHandle(function, out handle))
                {
                    throw new ArgumentException("function");
                }
                return _script.Operations.Invoke(handle, args);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(_script.GetService<ExceptionOperations>().FormatException(ex), ex);
            }
        }

        #endregion

        #region Copying

        public override AbstractSystem NewInstance()
        {
            var copy = (ScriptSystem) base.NewInstance();

            copy.Initialize();

            return copy;
        }

        #endregion

        #region Stream classes for script IO

        /// <summary>Writes informational messages from the scripting VM to the log.</summary>
        private sealed class InfoStreamWriter : System.IO.StreamWriter
        {
            public bool Enabled { get; set; }

            public InfoStreamWriter(System.IO.Stream stream)
                : base(stream)
            {
                Enabled = true;
            }

            public override void Write(string value)
            {
                if (Enabled && !string.IsNullOrWhiteSpace(value))
                {
                    Logger.Info(value);
                }
            }
        }

        /// <summary>Writes error messages from the scripting VM to the log.</summary>
        private sealed class ErrorStreamWriter : System.IO.StreamWriter
        {
            public bool Enabled { get; set; }

            public ErrorStreamWriter(System.IO.Stream stream)
                : base(stream)
            {
                Enabled = true;
            }

            public override void Write(string value)
            {
                if (Enabled && !string.IsNullOrWhiteSpace(value))
                {
                    Logger.Error(value);
                }
            }
        }

        #endregion
    }
}
