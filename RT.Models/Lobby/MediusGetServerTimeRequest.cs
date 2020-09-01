using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.GetServerTimeRequest)]
    public class MediusGetServerTimeRequest : BaseLobbyExtMessage
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.GetServerTimeRequest;



        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);
        }


        public override string ToString()
        {
            return base.ToString();
        }
    }
}
