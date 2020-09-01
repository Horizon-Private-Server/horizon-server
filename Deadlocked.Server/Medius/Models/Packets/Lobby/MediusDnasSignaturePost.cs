using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobbyExt, MediusLobbyExtMessageIds.DnasSignaturePost)]
    public class MediusDnasSignaturePost : BaseLobbyExtMessage
    {

		public override byte PacketType => (byte)MediusLobbyExtMessageIds.DnasSignaturePost;

        public string SessionKey; // SESSIONKEY_MAXLEN
        public MediusDnasCategory DnasSignatureType;
        public byte DnasSignatureLength;
        public byte[] DnasSignature = new byte[Constants.DNASSIGNATURE_MAXLEN];

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            SessionKey = reader.ReadString(Constants.SESSIONKEY_MAXLEN);
            reader.ReadBytes(2);
            DnasSignatureType = reader.Read<MediusDnasCategory>();
            DnasSignatureLength = reader.ReadByte();
            DnasSignature = reader.ReadBytes(Constants.DNASSIGNATURE_MAXLEN);
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(SessionKey, Constants.SESSIONKEY_MAXLEN);
            writer.Write(new byte[2]);
            writer.Write(DnasSignatureType);
            writer.Write(DnasSignatureLength);
            writer.Write(DnasSignature, Constants.DNASSIGNATURE_MAXLEN);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"SessionKey:{SessionKey}" + " " +
$"DnasSignatureType:{DnasSignatureType}" + " " +
$"DnasSignatureLength:{DnasSignatureLength}" + " " +
$"DnasSignature:{BitConverter.ToString(DnasSignature)}";
        }
    }
}
