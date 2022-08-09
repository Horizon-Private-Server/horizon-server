using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.ChannelList_ExtraInfo1)]
    public class MediusChannelList_ExtraInfoRequest1 : MediusChannelList_ExtraInfoRequest, IMediusRequest
    {
		public override byte PacketType => (byte)MediusLobbyExtMessageIds.ChannelList_ExtraInfo1;

    }
}
