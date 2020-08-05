using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.App
{
    [MediusApp(MediusAppPacketIds.FileDownloadResponse)]
    public class MediusFileDownloadResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.FileDownloadResponse;

        public byte[] Data = new byte[MediusConstants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE];
        public int iStartByteIndex;
        public int iDataSize;
        public int iPacketNumber;
        public int iXferStatus;
        public MediusCallbackStatus StatusCode;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            Data = reader.ReadBytes(MediusConstants.MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE);
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
            writer.Write(Data);
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