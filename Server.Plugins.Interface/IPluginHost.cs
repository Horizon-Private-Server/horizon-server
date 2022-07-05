﻿using DotNetty.Common.Internal.Logging;
using RT.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Interface
{
    public delegate Task OnRegisterActionHandler(PluginEvent eventType, object data);
    public delegate Task OnRegisterMessageActionHandler(RT_MSG_TYPE msgId, object data);
    public delegate Task OnRegisterMediusMessageActionHandler(NetMessageTypes msgClass, byte msgType, object data);

    public interface IPluginHost
    {
        void Log(InternalLogLevel level, string message);
        void Log(InternalLogLevel level, Exception ex);
        void Log(InternalLogLevel level, Exception ex, string message);

        void RegisterAction(PluginEvent eventType, OnRegisterActionHandler callback);
        void RegisterMessageAction(RT_MSG_TYPE msgId, OnRegisterMessageActionHandler callback);
        void RegisterMediusMessageAction(NetMessageTypes msgClass, byte msgType, OnRegisterMediusMessageActionHandler callback);
    }
}
