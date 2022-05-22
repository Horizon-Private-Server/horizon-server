using RT.Common;

namespace RT.Models
{
    public abstract class BaseMGCLMessage : BaseMediusMessage
    {
        public override NetMessageTypes PacketClass => NetMessageTypes.MessageClassLobbyReport;

        public BaseMGCLMessage()
        {

        }

    }
}