using RT.Common;

namespace RT.Models
{
    public abstract class BaseLobbyExtMessage : BaseMediusMessage
    {
        public override NetMessageClass PacketClass => NetMessageClass.MessageClassLobbyExt;

        public BaseLobbyExtMessage()
        {

        }
    }
}