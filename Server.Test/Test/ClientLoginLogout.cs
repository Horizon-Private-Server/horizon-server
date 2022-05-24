using DotNetty.Common.Internal.Logging;
using DotNetty.Transport.Channels;
using RT.Cryptography;
using RT.Models;
using Server.Test.Medius;
using System.Threading.Tasks;

namespace Server.Test.Test
{
    public class ClientLoginLogout : BaseClientConnect
    {
        static readonly IInternalLogger _logger = InternalLoggerFactory.GetInstance<ClientLoginLogout>();

        public override PS2_RSA AuthKey => Program.Settings.Medius.Key;

        public override int ApplicationId => 11184;

        protected override IInternalLogger Logger => _logger;


        public ClientLoginLogout(string serverIp, short serverPort) : base(serverIp, serverPort)
        {

        }

        protected override async Task OnDisconnected(IChannel channel)
        {

        }

        protected override async Task OnConnected(IChannel channel)
        {
            await base.OnConnected(channel);
        }

        protected override async Task ProcessMessage(BaseScertMessage message, IChannel channel)
        {
            await base.ProcessMessage(message, channel);
        }
    }
}
