using RT.Common;
using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.PlayerReport)]
    public class MediusPlayerReport : BaseLobbyMessage
    {

        public override byte PacketType => (byte)MediusLobbyMessageIds.PlayerReport;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public int MediusWorldID;
        public byte[] Stats = new byte[Constants.ACCOUNTSTATS_MAXLEN]; 

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(3);
            MediusWorldID = reader.ReadInt32();
            Stats = reader.ReadBytes(Constants.ACCOUNTSTATS_MAXLEN);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[3]);
            writer.Write(MediusWorldID);
            writer.Write(Stats, Constants.ACCOUNTSTATS_MAXLEN);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey} " +
$"MediusWorldID:{MediusWorldID} " +
$"Stats:{BitConverter.ToString(Stats)}";
        }
    }
}
