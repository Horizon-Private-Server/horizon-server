using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
    [MediusMessage(TypesAAA.GetIgnoreListResponse)]
    public class MediusGetIgnoreListResponse : BaseLobbyMessage
    {
        public class MediusGetIgnoreListResponseItem
        {
            public int IgnoreAccountID;
            public string IgnoreAccountName; // ACCOUNTNAME_MAXLEN
            public MediusPlayerStatus PlayerStatus;
        }

        public override TypesAAA MessageType => TypesAAA.GetIgnoreListResponse;

        public MediusCallbackStatus StatusCode;
        public int IgnoreAccountID;
        public string IgnoreAccountName; // ACCOUNTNAME_MAXLEN
        public MediusPlayerStatus PlayerStatus;
        public bool EndOfList;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            IgnoreAccountID = reader.ReadInt32();
            IgnoreAccountName = reader.ReadString(MediusConstants.ACCOUNTNAME_MAXLEN);
            PlayerStatus = reader.Read<MediusPlayerStatus>();
            EndOfList = reader.ReadBoolean();
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(IgnoreAccountID);
            writer.Write(IgnoreAccountName, MediusConstants.ACCOUNTNAME_MAXLEN);
            writer.Write(PlayerStatus);
            writer.Write(EndOfList);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"IgnoreAccountID:{IgnoreAccountID}" + " " +
$"IgnoreAccountName:{IgnoreAccountName}" + " " +
$"PlayerStatus:{PlayerStatus}" + " " +
$"EndOfList:{EndOfList}";
        }

        public static List<MediusGetIgnoreListResponse> FromCollection(string messageId, IEnumerable<MediusGetIgnoreListResponseItem> items)
        {
            List<MediusGetIgnoreListResponse> ignoreList = new List<MediusGetIgnoreListResponse>();

            foreach (var item in items)
            {
                ignoreList.Add(new MediusGetIgnoreListResponse()
                {
                    MessageID = messageId,
                    StatusCode = MediusCallbackStatus.MediusSuccess,
                    IgnoreAccountID = item.IgnoreAccountID,
                    IgnoreAccountName = item.IgnoreAccountName,
                    PlayerStatus = item.PlayerStatus
                });
            }

            // Set end of list
            ignoreList[ignoreList.Count - 1].EndOfList = true;

            return ignoreList;
        }
    }
}