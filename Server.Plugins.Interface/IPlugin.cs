using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Interface
{
    public interface IPlugin
    {

        Task Start(IPluginHost host);

    }
}
