using Microsoft.Extensions.Logging;
using RT.Common;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Server.Common.Logging
{
    public class LogSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public static LogSettings Singleton { get; set; } = null;

        /// <summary>
        /// Path to the log file.
        /// </summary>
        public string LogPath { get; set; } = "logs/medius.log";

        /// <summary>
        /// Whether to also log to the console.
        /// </summary>
        public bool LogToConsole { get; set; } = false;

        /// <summary>
        /// Size in bytes for each log file.
        /// </summary>
        public int RollingFileSize = 1024 * 1024 * 1;

        /// <summary>
        /// Max number of files before rolling back to the first log file.
        /// </summary>
        public int RollingFileCount = 100;

        /// <summary>
        /// Log level.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Collection of RT messages to print out
        /// </summary>
        public string[] RtLogFilter { get; set; } = Enum.GetNames(typeof(RT_MSG_TYPE));

        /// <summary>
        /// Collection of Medius Lobby messages to print out
        /// </summary>
        public string[] MediusLobbyLogFilter { get; set; } = Enum.GetNames(typeof(MediusLobbyMessageIds));

        /// <summary>
        /// Collection of Medius Lobby Ext messages to print out
        /// </summary>
        public string[] MediusLobbyExtLogFilter { get; set; } = Enum.GetNames(typeof(MediusLobbyExtMessageIds));

        /// <summary>
        /// Collection of Medius Lobby Ext messages to print out
        /// </summary>
        public string[] MediusMGCLLogFilter { get; set; } = Enum.GetNames(typeof(MediusMGCLMessageIds));

        /// <summary>
        /// Collection of Medius Lobby Ext messages to print out
        /// </summary>
        public string[] MediusDMEExtLogFilter { get; set; } = Enum.GetNames(typeof(MediusDmeMessageIds));

        /// <summary>
        /// Internal preprocessed collection of message id filters.
        /// </summary>
        private Dictionary<RT_MSG_TYPE, bool> _rtLogFilters = new Dictionary<RT_MSG_TYPE, bool>();
        private Dictionary<MediusDmeMessageIds, bool> _dmeLogFilters = new Dictionary<MediusDmeMessageIds, bool>();
        private Dictionary<MediusLobbyMessageIds, bool> _lobbyLogFilters = new Dictionary<MediusLobbyMessageIds, bool>();
        private Dictionary<MediusMGCLMessageIds, bool> _mgclLogFilters = new Dictionary<MediusMGCLMessageIds, bool>();
        private Dictionary<MediusLobbyExtMessageIds, bool> _lobbyExtLogFilters = new Dictionary<MediusLobbyExtMessageIds, bool>();

        /// <summary>
        /// Whether or not the given RT message id should be logged
        /// </summary>
        public bool IsLog(RT_MSG_TYPE msgType)
        {
            return _rtLogFilters.TryGetValue(msgType, out var r) && r;
        }

        /// <summary>
        /// Whether or not the given Medius lobby dme message id should be logged
        /// </summary>
        public bool IsLog(MediusDmeMessageIds msgType)
        {
            return _dmeLogFilters.TryGetValue(msgType, out var r) && r;
        }

        /// <summary>
        /// Whether or not the given Medius lobby message id should be logged
        /// </summary>
        public bool IsLog(MediusLobbyMessageIds msgType)
        {
            return _lobbyLogFilters.TryGetValue(msgType, out var r) && r;
        }

        /// <summary>
        /// Whether or not the given Medius game client library message id should be logged
        /// </summary>
        public bool IsLog(MediusMGCLMessageIds msgType)
        {
            return _mgclLogFilters.TryGetValue(msgType, out var r) && r;
        }

        /// <summary>
        /// Whether or not the given Medius lobby extension message id should be logged
        /// </summary>
        public bool IsLog(MediusLobbyExtMessageIds msgType)
        {
            return _lobbyExtLogFilters.TryGetValue(msgType, out var r) && r;
        }

        /// <summary>
        /// Does some post processing on the deserialized model.
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Load log filters in dictionary
            _rtLogFilters.Clear();
            if (RtLogFilter != null)
            {
                foreach (var filter in RtLogFilter)
                    _rtLogFilters.Add((RT_MSG_TYPE)Enum.Parse(typeof(RT_MSG_TYPE), filter), true);
            }

            _dmeLogFilters.Clear();
            if (MediusDMEExtLogFilter != null)
            {
                foreach (var filter in MediusDMEExtLogFilter)
                    _dmeLogFilters.Add((MediusDmeMessageIds)Enum.Parse(typeof(MediusDmeMessageIds), filter), true);
            }

            _lobbyLogFilters.Clear();
            if (MediusLobbyLogFilter != null)
            {
                foreach (var filter in MediusLobbyLogFilter)
                    _lobbyLogFilters.Add((MediusLobbyMessageIds)Enum.Parse(typeof(MediusLobbyMessageIds), filter), true);
            }

            _mgclLogFilters.Clear();
            if (MediusMGCLLogFilter != null)
            {
                foreach (var filter in MediusMGCLLogFilter)
                    _mgclLogFilters.Add((MediusMGCLMessageIds)Enum.Parse(typeof(MediusMGCLMessageIds), filter), true);
            }

            _lobbyExtLogFilters.Clear();
            if (MediusLobbyExtLogFilter != null)
            {
                foreach (var filter in MediusLobbyExtLogFilter)
                    _lobbyExtLogFilters.Add((MediusLobbyExtMessageIds)Enum.Parse(typeof(MediusLobbyExtMessageIds), filter), true);
            }
        }
    }
}
