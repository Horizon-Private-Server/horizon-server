using DotNetty.Common.Internal.Logging;
using RT.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Server.Plugins.Interface;

namespace Server.Plugins
{
    public class PluginsManager : IPluginHost
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<PluginsManager>();

        private ConcurrentDictionary<PluginEvent, List<Action<PluginEvent, object>>> _pluginCallbackInstances = new ConcurrentDictionary<PluginEvent, List<Action<PluginEvent, object>>>();
        private ConcurrentDictionary<RT_MSG_TYPE, List<Action<RT_MSG_TYPE, object>>> _pluginScertMessageCallbackInstances = new ConcurrentDictionary<RT_MSG_TYPE, List<Action<RT_MSG_TYPE, object>>>();
        private ConcurrentDictionary<(NetMessageTypes, byte), List<Action<NetMessageTypes, byte, object>>> _pluginMediusMessageCallbackInstances = new ConcurrentDictionary<(NetMessageTypes, byte), List<Action<NetMessageTypes, byte, object>>>();
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
            this._watcher = new FileSystemWatcher(this._pluginDir.FullName, "*.dll");
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

        #region On Event

        public Task OnEvent(PluginEvent eventType, object data)
        {
            if (!_pluginCallbackInstances.ContainsKey(eventType))
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                foreach (var callback in _pluginCallbackInstances[eventType])
                {
                    try
                    {
                        callback.Invoke(eventType, data);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"PLUGIN OnEvent Exception. {callback}({eventType}, {data})");
                        Logger.Error(e);
                    }
                }
            });
        }

        public void OnMessageEvent(RT_MSG_TYPE msgId, object data)
        {
            if (!_pluginScertMessageCallbackInstances.ContainsKey(msgId))
                return;

            foreach (var callback in _pluginScertMessageCallbackInstances[msgId])
            {
                try
                {
                    callback.Invoke(msgId, data);
                }
                catch (Exception e)
                {
                    Logger.Error($"PLUGIN OnEvent Exception. {callback}({msgId}, {data})");
                    Logger.Error(e);
                }
            }
        }

        public void OnMediusMessageEvent(NetMessageTypes msgClass, byte msgType, object data)
        {
            var key = (msgClass, msgType);
            if (!_pluginMediusMessageCallbackInstances.ContainsKey(key))
                return;

            foreach (var callback in _pluginMediusMessageCallbackInstances[key])
            {
                try
                {
                    callback.Invoke(msgClass, msgType, data);
                }
                catch (Exception e)
                {
                    Logger.Error($"PLUGIN OnEvent Exception. {callback}({key}, {data})");
                    Logger.Error(e);
                }
            }
        }

        #endregion

        #region Register Event

        public void RegisterAction(PluginEvent eventType, Action<PluginEvent, object> callback)
        {
            List<Action<PluginEvent, object>> callbacks;
            if (!_pluginCallbackInstances.ContainsKey(eventType))
                _pluginCallbackInstances.TryAdd(eventType, callbacks = new List<Action<PluginEvent, object>>());
            else
                callbacks = _pluginCallbackInstances[eventType];


            callbacks.Add(callback);
        }

        public void RegisterMessageAction(RT_MSG_TYPE msgId, Action<RT_MSG_TYPE, object> callback)
        {
            List<Action<RT_MSG_TYPE, object>> callbacks;
            if (!_pluginScertMessageCallbackInstances.ContainsKey(msgId))
                _pluginScertMessageCallbackInstances.TryAdd(msgId, callbacks = new List<Action<RT_MSG_TYPE, object>>());
            else
                callbacks = _pluginScertMessageCallbackInstances[msgId];


            callbacks.Add(callback);
        }

        public void RegisterMediusMessageAction(NetMessageTypes msgClass, byte msgType, Action<NetMessageTypes, byte, object> callback)
        {
            List<Action<NetMessageTypes, byte, object>> callbacks;
            var key = (msgClass, msgType);
            if (!_pluginMediusMessageCallbackInstances.ContainsKey(key))
                _pluginMediusMessageCallbackInstances.TryAdd(key, callbacks = new List<Action<NetMessageTypes, byte, object>>());
            else
                callbacks = _pluginMediusMessageCallbackInstances[key];


            callbacks.Add(callback);
        }

        #endregion

        #region Log

        public void Log(InternalLogLevel level, string message)
        {
            Logger.Log(level, message);
        }

        public void Log(InternalLogLevel level, Exception ex)
        {
            Logger.Log(level, ex);
        }

        public void Log(InternalLogLevel level, Exception ex, string message)
        {
            Logger.Log(level, message, ex);
        }

        #endregion

        private void reloadPlugins()
        {
            // Clear cache
            _pluginCallbackInstances.Clear();
            _pluginScertMessageCallbackInstances.Clear();
            _pluginMediusMessageCallbackInstances.Clear();

            // 
            Logger.Warn($"Reloading plugins");

            // Ensure valid plugins directory
            if (!this._pluginDir.Exists)
                return;

            // Add assemblies
            foreach (var file in this._pluginDir.GetFiles("*.dll", SearchOption.AllDirectories))
            {
                Assembly pluginAssembly = Assembly.LoadFile(file.FullName);
                Type pluginInterface = typeof(IPlugin);
                var plugins = pluginAssembly.GetTypes()
                    .Where(type => pluginInterface.IsAssignableFrom(type));

                foreach (var plugin in plugins)
                {
                    IPlugin instance = (IPlugin)Activator.CreateInstance(plugin);

                    _ = instance.Start(this);
                }
            }
        }
    }
}
