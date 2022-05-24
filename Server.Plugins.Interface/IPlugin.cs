using System.Threading.Tasks;

namespace Server.Plugins.Interface
{
    public interface IPlugin
    {

        Task Start(string workingDirectory, IPluginHost host);

    }
}