﻿using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.IgnoreSetListRequest)]
    public class MediusIgnoreSetListRequest : BaseLobbyMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyExtMessageIds.IgnoreSetListRequest;

        public MessageId MessageID { get; set; }

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int NumEntries;
        public string[] List;

        public byte NAME_LEN;

        public override void Deserialize(Server.Common.Stream.MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            NumEntries = reader.ReadInt32();
            
            
            
            List = new string[NumEntries];
            for (int i = 0; i < NumEntries; i++) {
                NAME_LEN = reader.ReadByte();
                List[i] = reader.ReadString(NAME_LEN);
            }
        }

        public override void Serialize(Server.Common.Stream.MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(NumEntries);
            for (int i = 0; i < NumEntries; i++)
            {
                writer.Write(NAME_LEN);
                writer.Write(List[i]);
            }
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID: {MessageID} " +
                $"SessionKey: {SessionKey} " +
                $"NumEntries: {NumEntries} " +
                $"List: {Convert.ToString(List)}";
        }
    }
}