﻿using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.SetGameListFilter0)]
    public class MediusSetGameListFilterRequest0 : BaseLobbyMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.SetGameListFilter0;

        public MessageId MessageID { get; set; }

        public string SessionKey;
        public MediusGameListFilterField FilterField;
        public MediusComparisonOperator ComparisonOperator;
        public int BaselineValue;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            FilterField = reader.Read<MediusGameListFilterField>();
            ComparisonOperator = reader.Read<MediusComparisonOperator>();
            BaselineValue = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(FilterField);
            writer.Write(ComparisonOperator);
            writer.Write(BaselineValue);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID} " +
                $"SessionKey:{SessionKey} " +
                $"FilterField:{FilterField} " +
                $"ComparisonOperator:{ComparisonOperator} " +
                $"BaselineValue:{BaselineValue}";
        }
    }
}