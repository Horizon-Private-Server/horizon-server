using RT.Common;

namespace RT.Models
{
    public abstract class BaseMGCLMessage : BaseMediusMessage
    {
        public override NetMessageClass PacketClass => NetMessageClass.MessageClassLobbyReport;

        public BaseMGCLMessage()
        {

        }

    }
}