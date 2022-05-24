﻿using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobby, MediusLobbyMessageIds.GameList_ExtraInfo0)]
    public class MediusGameList_ExtraInfoRequest0 : BaseLobbyMessage, IMediusRequest
    {

        public override byte PacketType => (byte)MediusLobbyMessageIds.GameList_ExtraInfo0;

        public MessageId MessageID { get; set; }

        public ushort PageID;
        public ushort PageSize;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            //
            reader.ReadBytes(1);
            PageID = reader.ReadUInt16();
            PageSize = reader.ReadUInt16();
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(new byte[1]);
            writer.Write(PageID);
            writer.Write(PageSize);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"PageID:{PageID} " +
                $"PageSize:{PageSize}";
        }
    }
}
