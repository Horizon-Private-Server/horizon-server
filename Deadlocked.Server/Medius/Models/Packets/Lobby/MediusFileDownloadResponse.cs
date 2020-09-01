using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Medius.Models.Packets.Lobby
{
	[MediusMessage(NetMessageTypes.MessageClassLobby, MediusLobbyMessageIds.FileDownloadResponse)]
    public class MediusFileDownloadResponse : BaseLobbyMessage
    {

		public override byte PacketType => (byte)MediusLobbyMessageIds.FileDownloadResponse;

        public byte[] Data = new byte[Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE];
        public int iStartByteIndex;
        public int iDataSize;
        public int iPacketNumber;
        public int iXferStatus;
        public MediusCallbackStatus StatusCode;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            Data = reader.ReadBytes(Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE);
            iStartByteIndex = reader.ReadInt32();
            iDataSize = reader.ReadInt32();
            iPacketNumber = reader.ReadInt32();
            iXferStatus = reader.ReadInt32();
            StatusCode = reader.Read<MediusCallbackStatus>();

            // 
            base.Deserialize(reader);
            reader.ReadBytes(3);
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            writer.Write(Data, Constants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE);
            writer.Write(iStartByteIndex);
            writer.Write(iDataSize);
            writer.Write(iPacketNumber);
            writer.Write(iXferStatus);
            writer.Write(StatusCode);

            // 
            base.Serialize(writer);
            writer.Write(new byte[3]);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"Data:{Data}" + " " +
$"iStartByteIndex:{iStartByteIndex}" + " " +
$"iDataSize:{iDataSize}" + " " +
$"iPacketNumber:{iPacketNumber}" + " " +
$"iXferStatus:{iXferStatus}" + " " +
$"StatusCode:{StatusCode}";
        }
    }
}
