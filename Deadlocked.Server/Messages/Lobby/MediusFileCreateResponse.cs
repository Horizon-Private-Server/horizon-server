using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.FileCreateResponse)]
    public class MediusFileCreateResponse : BaseAppMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.FileCreateResponse;

        public MediusFile MediusFileInfo;
        public MediusCallbackStatus StatusCode;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MediusFileInfo = reader.Read<MediusFile>();
            StatusCode = reader.Read<MediusCallbackStatus>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MediusFileInfo);
            writer.Write(StatusCode);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"MediusFileInfo:{MediusFileInfo}" + " " +
$"StatusCode:{StatusCode}";
        }
    }
}