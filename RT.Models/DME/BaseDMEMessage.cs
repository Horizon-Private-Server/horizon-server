using RT.Common;

namespace RT.Models
{
    public abstract class BaseDMEMessage : BaseMediusMessage
    {
        public override NetMessageTypes PacketClass => NetMessageTypes.MessageClassDME;

        public BaseDMEMessage()
        {

        }
    }
}