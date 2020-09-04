using DotNetty.Common.Internal.Logging;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Plugins
{
    public class PluginsManager
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<PluginsManager>();

        private ScriptEngine _engine = null;
        private ScriptScope _scope = null;
        private ConcurrentDictionary<PluginEvent, List<object>> _pluginCallbackInstances = new ConcurrentDictionary<PluginEvent, List<object>>();


        public PluginsManager(string pluginsDirectory)
        {
            // Ensure valid plugins directory
            var dirInfo = new DirectoryInfo(pluginsDirectory);
            if (!dirInfo.Exists)
                return;

            // Create python engine
            _engine = Python.CreateEngine();
            _scope = _engine.CreateScope();

            // Add assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                _engine.Runtime.LoadAssembly(assembly);

            // 
            var searchPaths = _engine.GetSearchPaths();
            searchPaths.Add(dirInfo.FullName);
            _engine.SetSearchPaths(searchPaths);

            // Set some global variables
            Action<int, object> logAction = (l, m) => { log((InternalLogLevel)l, m); };
            _scope.SetVariable("log", logAction);

            Action<PluginEvent, object> registerAction = (t, c) => { registerEventHandler(t, c); };
            _scope.SetVariable("registerEventHandler", registerAction);

            // Change directory to plugins folder
            _engine.Execute($"import os\nos.chdir(\"{dirInfo.FullName.Replace("\\", "/")}\")", _scope);

            // Gather all plugins
            foreach (var pyFile in Directory.GetFiles(dirInfo.FullName, "*.py", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    ScriptSource source = _engine.CreateScriptSourceFromFile(pyFile);

                    source.Execute(_scope);
                }
                catch (Exception e)
                {
                    Logger.Error(_engine.GetService<ExceptionOperations>().FormatException(e));
                }
            }
        }

        public void OnEvent(PluginEvent eventType, object data)
        {
            if (!_pluginCallbackInstances.ContainsKey(eventType))
                return;

            foreach (var callbacks in _pluginCallbackInstances[eventType])
            {
                try
                {
                    _engine.Operations.Invoke(callbacks, eventType, data);
                }
                catch (Exception e)
                {
                    Logger.Error($"PLUGIN OnEvent Exception. {callbacks}({eventType}, {data})");
                    Logger.Error(_engine.GetService<ExceptionOperations>().FormatException(e));
                }
            }
        }

        private void registerEventHandler(PluginEvent eventType, object callback)
        {
            List<object> callbacks;
            if (!_pluginCallbackInstances.ContainsKey(eventType))
                _pluginCallbackInstances.TryAdd(eventType, callbacks = new List<object>());
            else
                callbacks = _pluginCallbackInstances[eventType];


            callbacks.Add(callback);
        }

        private void log(InternalLogLevel logLevel, object msg)
        {
            Logger.Log(logLevel, msg?.ToString());
        }
    }
}
