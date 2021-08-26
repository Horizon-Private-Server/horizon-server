using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models.Lobby
{
    [MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.RemovePlayerFromClan)]
    public class MediusRemovePlayerFromClanRequest : BaseLobbyMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.RemovePlayerFromClan;



        public MessageId MessageID { get; set; }
        public string SessionKey; // SESSIONKEY_MAXLEN
        public int PlayerAccountID;
        public int ClanID;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MessageID = reader.Read<MessageId>();
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            PlayerAccountID = reader.ReadInt32();
            ClanID = reader.ReadInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MessageID ?? MessageId.Empty);
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(PlayerAccountID);
            writer.Write(ClanID);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MessageID:{MessageID}" + " " +
                $"SessionKey:{SessionKey}" + " " +
                $"PlayerAccountID:{PlayerAccountID}" + " " +
                $"ClanID:{ClanID}";
        }
    }
}