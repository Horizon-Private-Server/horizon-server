using Server.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RT.Models
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.FileDownload)]
    public class MediusFileDownloadRequest : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.FileDownload;

        public MediusFile MediusFileInfo;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            MediusFileInfo = reader.Read<MediusFile>();

            // 
            reader.ReadBytes(4);
            base.Deserialize(reader);
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            writer.Write(MediusFileInfo);

            // 
            writer.Write(new byte[4]);
            base.Serialize(writer);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"MediusFileInfo:{MediusFileInfo}";
        }
    }
}
