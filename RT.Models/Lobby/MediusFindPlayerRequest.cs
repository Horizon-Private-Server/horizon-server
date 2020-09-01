using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.FindPlayer)]
    public class MediusFindPlayerRequest : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.FindPlayer;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusPlayerSearchType SearchType;
        public int ID;
        public string Name; // PLAYERNAME_MAXLEN

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            SearchType = reader.Read<MediusPlayerSearchType>();
            ID = reader.ReadInt32();
            Name = reader.ReadString(Constants.PLAYERNAME_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(SearchType);
            writer.Write(ID);
            writer.Write(Name, Constants.PLAYERNAME_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey} " +
$"SearchType:{SearchType} " +
$"ID:{ID} " +
$"Name:{Name}";
        }
    }
}
