using DotNetty.Common.Internal.Logging;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins
{
    public class PluginsManager
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<PluginsManager>();

        private ScriptEngine _engine = null;
        private ScriptScope _scope = null;
        private ConcurrentDictionary<PluginEvent, List<object>> _pluginCallbackInstances = new ConcurrentDictionary<PluginEvent, List<object>>();
        private bool _reload = false;
        private DirectoryInfo _pluginDir = null;
        private FileSystemWatcher _watcher = null;

        public PluginsManager(string pluginsDirectory)
        {
            // Ensure valid plugins directory
            this._pluginDir = new DirectoryInfo(pluginsDirectory);
            if (!this._pluginDir.Exists)
                return;

            // Add a watcher so we can auto reload the plugins on change
            this._watcher = new FileSystemWatcher(this._pluginDir.FullName, "*.py");
            this._watcher.IncludeSubdirectories = true;
            this._watcher.Changed += (s, e) => { this._reload = true; };
            this._watcher.Renamed += (s, e) => { this._reload = true; };
            this._watcher.Created += (s, e) => { this._reload = true; };
            this._watcher.Deleted += (s, e) => { this._reload = true; };
            this._watcher.EnableRaisingEvents = true;

            reloadPlugins();
        }

        public void Tick()
        {
            if (this._reload)
            {
                this._reload = false;
                reloadPlugins();
            }

            OnEvent(PluginEvent.TICK, null);
        }

        public void OnEvent(PluginEvent eventType, object data)
        {
            if (!_pluginCallbackInstances.ContainsKey(eventType))
                return;

            Task.Run(() =>
            {
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
            });
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

        private void reloadPlugins()
        {
            // Clear cache
            _engine = null;
            _scope = null;
            _pluginCallbackInstances.Clear();

            // 
            Logger.Warn($"Reloading plugins");

            // Ensure valid plugins directory
            if (!this._pluginDir.Exists)
                return;

            // Create python engine
            _engine = Python.CreateEngine();
            _scope = _engine.CreateScope();

            // Add assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                _engine.Runtime.LoadAssembly(assembly);


            // 
            var searchPaths = _engine.GetSearchPaths();
            searchPaths.Add(this._pluginDir.FullName);
            _engine.SetSearchPaths(searchPaths);

            // Set some global variables
            var module = _engine.GetBuiltinModule();

            Action<int, object> logAction = (l, m) => { log((InternalLogLevel)l, m); };
            module.SetVariable("log", logAction);

            Action<PluginEvent, object> registerAction = (t, c) => { registerEventHandler(t, c); };
            module.SetVariable("registerEventHandler", registerAction);

            // Gather all plugins
            foreach (var pyFile in Directory.GetFiles(this._pluginDir.FullName, "pluginstart.py", SearchOption.AllDirectories))
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
    }
}
