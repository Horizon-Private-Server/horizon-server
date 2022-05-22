using System.Threading.Tasks;

namespace Server.Medius
{
    public interface IMediusComponent
    {
        int Port { get; }

        void Start();
        Task Stop();

        Task Tick();
    }
}