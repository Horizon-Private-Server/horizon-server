using DotNetty.Common.Internal.Logging;
using RT.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Plugins.Interface
{
    public interface IPluginHost
    {
        void Log(InternalLogLevel level, string message);
        void Log(InternalLogLevel level, Exception ex);
        void Log(InternalLogLevel level, Exception ex, string message);

        void RegisterAction(PluginEvent eventType, Action<PluginEvent, object> callback);
        void RegisterMessageAction(RT_MSG_TYPE msgId, Action<RT_MSG_TYPE, object> callback);
        void RegisterMediusMessageAction(NetMessageTypes msgClass, byte msgType, Action<NetMessageTypes, byte, object> callback);
    }
}
